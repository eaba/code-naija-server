using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neo;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.Wallets;

namespace NeoFullNode
{
    public class Program
    {
       
        public static Wallet Wallet;
        public static void Main(string[] args)
        {
            byte[] script = textBox8.Text.HexToBytes();
            byte[] parameter_list = "07,10".HexToBytes();
            var version = "1.0";
            var name = "Election 2019";
            var description = "Testing smartcontract";
            var author = "Ebere Abanonu";
            var email = "eabanonu@yahoo.com";
            ContractParameterType return_type = ContractParameterType.Boolean;
            ContractPropertyState properties = ContractPropertyState.HasStorage;
            CreateHostBuilder(args).Build().Run();
            
        }
          public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
    }
}
