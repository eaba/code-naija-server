using Akka.Actor;
using Commands;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Neo;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NeoFullNode
{
    public class Decision
    {
        private NeoSystem _system;
        public UInt160 ChangeAddress; //=> ((string)comboBoxChangeAddress.SelectedItem).ToScriptHash();
        public UInt160 FromAddress;
        public Decision(NeoSystem system)
        {
            _system = system;
        }
        private void HandleCommand(PulsarCommand command)
        {
            switch(command.Command)
            {

            }
        }
        
        private (bool, Dictionary<string, string>) OnCreateAccount()
        {
            try
            {
                WalletAccount account = Program.Wallet.CreateAccount();
                return (true, new Dictionary<string, string> { { "Address", account.Address }, { "Public", account.GetKey().PublicKey.EncodePoint(true).ToHexString() }, { "Private", account.GetKey().PrivateKey.ToHexString()} });
            }
            catch (Exception ex)
            {
                return (false, null);
            }
        }
        private (bool, Dictionary<string, string>) OnRequestIdentity(Dictionary<string, string> command)
        {
            try
            {
                var script = "";
                var pub = command["Public"];
                var addr = command["Address"];
                var (success, message) = OnInvokeScript(script, "RequestIdentity", new[] { pub, addr });
                return (success, new Dictionary<string, string> { { "Message", message }, { "Success", success.ToString() } });
            }
            catch (Exception ex)
            {
                return (false, null);
            }
        }
        public void OnDeployContract(Fixed8 fee, byte[] script, byte[] parameter_list, ContractParameterType return_type, ContractPropertyState properties, string name, string version, string author, string email, string description)
        {
            try
            {
                InvocationTransaction tx = null;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitSysCall("Neo.Contract.Create", script, parameter_list, return_type, properties, name, version, author, email, description);
                    tx = new InvocationTransaction
                    {
                        Script = sb.ToArray()
                    };
                }
                InvocationTransaction result = Program.Wallet.MakeTransaction(new InvocationTransaction
                {
                    Version = tx.Version,
                    Script = tx.Script,
                    Gas = tx.Gas,
                    Attributes = tx.Attributes,
                    Outputs = tx.Outputs
                }, change_address: null, fee: fee);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        private (bool, Dictionary<string, string>) OnCreateIdentity(Dictionary<string, string> command)
        {
            try
            {
                var script = "";
                var pub = command["Public"];
                var addr = command["Address"];
                HashAlgorithm algorithm = SHA256.Create();
                var id = Encoding.UTF8.GetString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(pub+addr)));
                var (success, message) = OnInvokeScript(script, "CreateIdentity", new[]{pub, addr, id});
                return (success, new Dictionary<string, string> { { "Message", message }, { "Success", success.ToString()} });
            }
            catch (Exception ex)
            {
                return (false, null);
            }
        }
        private (bool, Dictionary<string, string>) Onvote(Dictionary<string, string> command)
        {
            try
            {
                var script = "";
                var voter = command["Voter"];
                var party = command["Party"];
                var (success, message) = OnInvokeScript(script, "Vote", new[] {voter, party });
                return (success, new Dictionary<string, string> { { "Message", message }, { "Success", success.ToString() } });
            }
            catch (Exception ex)
            {
                return (false, null);
            }
        }
        //invoke election smart contract
        private (bool, Dictionary<string, string>) OnAddCandidate(Dictionary<string, string> command)
        {
            try
            {
                var script = "";
                var pub = command["Public"];
                var addr = command["Address"];
                var name = command["Name"];
                var party = command["Party"];

                var (success, message) = OnInvokeScript(script, "AddCandidate", new[] { pub, addr, name, party });
                return (success, new Dictionary<string, string> { { "Message", message }, { "Success", success.ToString() } });
            }
            catch (Exception ex)
            {
                return (false, null);
            }
        }
        
        private (bool, string) OnInvokeScript(string script, string operation, string[] args)
        {
            var scriptHash = UInt160.Parse(script);

            List<ContractParameter> contractParameters = new List<ContractParameter>();
            for (int i = 0; i < args.Length; i++)
            {
                contractParameters.Add(new ContractParameter()
                {
                    // TODO: support contract params of type other than string.
                    Type = ContractParameterType.String,
                    Value = args[i]
                });
            }

            ContractParameter[] parameters =
            {
                new ContractParameter
                {
                    Type = ContractParameterType.String,
                    Value = operation
                },
                new ContractParameter
                {
                    Type = ContractParameterType.Array,
                    Value = contractParameters.ToArray()
                }
            };

            var tx = new InvocationTransaction();

            using (ScriptBuilder scriptBuilder = new ScriptBuilder())
            {
                scriptBuilder.EmitAppCall(scriptHash, parameters);
                Console.WriteLine($"Invoking script with: '{scriptBuilder.ToArray().ToHexString()}'");
                tx.Script = scriptBuilder.ToArray();
            }

            if (tx.Attributes == null) tx.Attributes = new TransactionAttribute[0];
            if (tx.Inputs == null) tx.Inputs = new CoinReference[0];
            if (tx.Outputs == null) tx.Outputs = new TransactionOutput[0];
            if (tx.Witnesses == null) tx.Witnesses = new Witness[0];
            ApplicationEngine engine = ApplicationEngine.Run(tx.Script, tx);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"VM State: {engine.State}");
            sb.AppendLine($"Gas Consumed: {engine.GasConsumed}");
            sb.AppendLine($"Evaluation Stack: {new JArray(engine.ResultStack.Select(p => p.ToParameter().ToJson()))}");
            Console.WriteLine(sb.ToString());
            if (engine.State.HasFlag(VMState.FAULT))
            {
                Console.WriteLine("Engine faulted.");
                return (true, "Execution failed!");
            }
            if (NoWallet()) return (true, "Execution failed!");
            tx = DecorateInvocationTransaction(tx);
            if (tx == null)
            {
                Console.WriteLine("Error: insufficient balance.");
                return (true, "Execution failed!");
            }
            return (SignAndSendTx(tx), "Script ran");
        }
        private static bool NoWallet()
        {
            if (Program.Wallet != null) return false;
            Console.WriteLine("You have to open the wallet first.");
            return true;
        }
        public InvocationTransaction DecorateInvocationTransaction(InvocationTransaction tx)
        {
            Fixed8 fee = Fixed8.Zero;

            if (tx.Size > 1024)
            {
                fee = Fixed8.FromDecimal(0.001m);
                fee += Fixed8.FromDecimal(tx.Size * 0.00001m);
            }

            return Program.Wallet.MakeTransaction(new InvocationTransaction
            {
                Version = tx.Version,
                Script = tx.Script,
                Gas = tx.Gas,
                Attributes = tx.Attributes,
                Inputs = tx.Inputs,
                Outputs = tx.Outputs
            }, fee: fee);
        }
        public bool SignAndSendTx(InvocationTransaction tx)
        {
            ContractParametersContext context;
            try
            {
                context = new ContractParametersContext(tx);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error creating contract params: {ex}");
                throw;
            }
            Program.Wallet.Sign(context);
            string msg;
            if (context.Completed)
            {
                tx.Witnesses = context.GetWitnesses();
                Program.Wallet.ApplyTransaction(tx);

                _system.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });

                msg = $"Signed and relayed transaction with hash={tx.Hash}";
                Console.WriteLine(msg);
                return true;
            }

            msg = $"Failed sending transaction with hash={tx.Hash}";
            Console.WriteLine(msg);
            return true;
        }
        
        class StartConsming
        {

        }
        
    }
}
