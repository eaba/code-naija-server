using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectorNode
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ICluster _cluster;
        private ISession _session;
        private IConsumer _consumer;
        public Worker(ILogger<Worker> logger)
        {
            _cluster = Cluster.Builder().AddContactPoint("127.0.0.1").Build();
            // create session
            _session = _cluster.Connect();
            _logger = logger;
        }
        private void SetupTables()
        {

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var client = PulsarClient.Builder().Build();
            _consumer = client.NewConsumer()
                  .SubscriptionName("ProjectorSubscription")
                  .Topic("persistent://public/default/smartcontract")
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
    }
}
