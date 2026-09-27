using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO.Pipelines;

namespace MyKafkaSystem.Producer;

public class Producer(ILogger<Producer> logger) : BackgroundService
{
    private readonly ILogger<Producer> _logger;
    private readonly IProducer<Null, string> _kafkaProducer;

    public Producer(ILogger<Producer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootStrapServers = "localhost:9092",
            Acks = Acks.All
        };
        _kafkaProducer = new ProducerBuilder<Null, string>(config).Build();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Producer running at: {time}", DateTimeOffset.Now);
            }

            try
            {
                var message = new Message<Null, string>
                {
                    Value = $"Hello Kafka! {DateTimeOffset.Now}"
                };
                DeliveryResult<Null, string> ReadResult = await _kafkaProducer.ProduceAsync(
                    "my-topic",
                    message,
                    stoppingToken
                );
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Kafka delivery failed: {Reason}", ex.Error.Reason);
            }



            _producer.Produce("my-topic", new Message<Null, string> {Value = "hello world"}, handler);
            await Task.Delay(1000, stoppingToken);
        }
    }

    public overrride void Dispose()
    {
        _kafkaProducer.Flush(TimeSpan.FromSeconds(5));
        _kafkaProducer.Dispose();
        base.Dispose();
    }
}
