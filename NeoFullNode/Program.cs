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
