using Akka.Actor;
using Neo;

using Neo.Persistence.LevelDB;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using Neo.Wallets.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NeoFullNode
{
    public class MainService
    {
        private LevelDBStore _store;
        private NeoSystem _system;
        private WalletIndexer indexer;
        private IActorRef _actorRef;
        public MainService()
        {

        }
        private WalletIndexer GetIndexer()
        {
            if (indexer is null)
                indexer = new WalletIndexer(Settings.Default.Paths.Index);
            return indexer;
        }

        private static bool NoWallet()
        {
            if (Program.Wallet != null) return false;
            Console.WriteLine("You have to open the wallet first.");
            return true;
        }
        public NeoSystem StartUpNode()
        {
            _store = new LevelDBStore(Path.GetFullPath(Settings.Default.Paths.Chain));
            _system = new NeoSystem(_store);
            _system.StartNode(
                port: Settings.Default.P2P.Port,
                wsPort: Settings.Default.P2P.WsPort,
                minDesiredConnections: Settings.Default.P2P.MinDesiredConnections,
                maxConnections: Settings.Default.P2P.MaxConnections,
                maxConnectionsPerAddress: Settings.Default.P2P.MaxConnectionsPerAddress);
            if (Settings.Default.UnlockWallet.IsActive)
            {
                try
                {
                    Program.Wallet = OpenWallet(GetIndexer(), Settings.Default.UnlockWallet.Path, Settings.Default.UnlockWallet.Password);
                }
                catch (CryptographicException)
                {
                    Console.WriteLine($"failed to open file \"{Settings.Default.UnlockWallet.Path}\"");
                }
                if (Settings.Default.UnlockWallet.StartConsensus && Program.Wallet != null)
                {
                    OnStartConsensusCommand(null);
                }
            }
            return _system;
        }
        private bool OnStartCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "consensus":
                    return OnStartConsensusCommand(args);
                default:
                    return false;
            }
        }
        private static Wallet OpenWallet(WalletIndexer indexer, string path, string password)
        {
            if (Path.GetExtension(path) == ".db3")
            {
                return UserWallet.Open(indexer, path, password);
            }
            else
            {
                NEP6Wallet nep6wallet = new NEP6Wallet(indexer, path);
                nep6wallet.Unlock(password);
                return nep6wallet;
            }
        }
        private bool OnStartConsensusCommand(string[] args)
        {
            if (NoWallet()) return true;
            _system.StartConsensus(Program.Wallet);
            return true;
        }

        public void ShutDownNode()
        {
            _system.Dispose();
            _store.Dispose();
        }
    }
}
