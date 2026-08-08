using Confluent.Kafka;
using System.Net;
using System.Reflection;

namespace MyKafkaSystem.Producer;

public class Producer(ILogger<Producer> logger) : BackgroundService
{
    private object _producer;
    public Producer()
    {
        var _producer = new ProducerBuilder<NullabilityInfo, string>(config).Build();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Producer running at: {time}", DateTimeOffset.Now);
            }
            _producer.Produce("my-topic", new Message<Null, string> {Value = "hello world"}, handler)
            await Task.Delay(1000, stoppingToken);
        }
    }
}
