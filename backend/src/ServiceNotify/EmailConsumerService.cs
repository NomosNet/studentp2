using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StudentPass.Contracts.Messages;

namespace ServiceNotify;

public sealed class EmailConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly EmailSender _emailSender;
    private readonly ILogger<EmailConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public EmailConsumerService(IConfiguration configuration, EmailSender emailSender, ILogger<EmailConsumerService> logger)
    {
        _configuration = configuration;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        if (_channel is null)
        {
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonSerializer.Deserialize<EmailMessage>(json);
                if (message is null || string.IsNullOrWhiteSpace(message.Email))
                {
                    _logger.LogWarning("Invalid email payload: {Payload}", json);
                    _channel.BasicAck(args.DeliveryTag, false);
                    return;
                }

                _logger.LogInformation("Received email for {Email}", message.Email);
                await _emailSender.SendAsync(message.Email, message.FullName, message.Subject, message.Message, stoppingToken);
                _channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process email message");
                _channel.BasicNack(args.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume("email_queue", autoAck: false, consumer);
        _logger.LogInformation("ServiceNotify is listening on email_queue");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "rabbitmq",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = _configuration["RabbitMQ:User"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        for (var attempt = 1; attempt <= 30 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.QueueDeclare("email_queue", durable: true, exclusive: false, autoDelete: false);
                _logger.LogInformation("Connected to RabbitMQ");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ connect attempt {Attempt}/30 failed: {Error}", attempt, ex.Message);
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogError("Could not connect to RabbitMQ");
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
