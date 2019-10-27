using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo.SmartContract;

namespace NeoFullNode
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private static IProducer _producer;
        private IConsumer _consumer;
        private static JsonSerializerOptions _options;
        private Decision _decision;
        public Worker()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            NeoService.Notify += NeoService_Notify;
            var neo = new MainService().StartUpNode();
            _decision = new Decision(neo);
            await using var client = PulsarClient.Builder().Build();
            _producer = client.NewProducer()
                 .Topic("persistent://public/default/smartcontract")
                 .Create();
            _consumer = client.NewConsumer()
                  .SubscriptionName("DecisionSubscription")
                  .Topic("Decision")
                  .Create();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await foreach (var message in _consumer.Messages())
                {
                    Console.WriteLine("Received: " + Encoding.UTF8.GetString(message.Data.ToArray()));

                    await _consumer.Acknowledge(message);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
        private static void NeoService_Notify(object sender, NotifyEventArgs e)
        {
            Task.Factory.StartNew(async () =>
            {
                var state = e.State;
                var t = JsonSerializer.Serialize(state, _options);
                _ = await _producer.Send(Encoding.UTF8.GetBytes(t)); //send projector to project into cassandra
            });
        }
    }
}
