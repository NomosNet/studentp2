using ServiceNotify;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddHostedService<EmailConsumerService>();

var host = builder.Build();
host.Run();
