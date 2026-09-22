using System.Text.Json;
using RabbitMQ.Client;
using StudentPass.Contracts.Messages;

namespace ServiceUsers.Services;

public sealed class EmailQueuePublisher : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailQueuePublisher> _logger;
    private readonly object _sync = new();
    private IConnection? _connection;
    private IModel? _channel;

    public EmailQueuePublisher(IConfiguration configuration, ILogger<EmailQueuePublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void Publish(EmailMessage message)
    {
        EnsureConnected();
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        _channel!.BasicPublish(exchange: string.Empty, routingKey: "email_queue", basicProperties: null, body: body);
    }

    private void EnsureConnected()
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        lock (_sync)
        {
            if (_channel is { IsOpen: true })
            {
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "rabbitmq",
                Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
                UserName = _configuration["RabbitMQ:User"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                AutomaticRecoveryEnabled = true
            };

            Exception? last = null;
            for (var attempt = 1; attempt <= 30; attempt++)
            {
                try
                {
                    _connection?.Dispose();
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.QueueDeclare("email_queue", durable: true, exclusive: false, autoDelete: false);
                    _logger.LogInformation("Connected to RabbitMQ, queue email_queue declared");
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    _logger.LogWarning("RabbitMQ connect attempt {Attempt}/30 failed: {Error}", attempt, ex.Message);
                    Thread.Sleep(2000);
                }
            }

            throw last ?? new InvalidOperationException("Failed to connect to RabbitMQ");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
