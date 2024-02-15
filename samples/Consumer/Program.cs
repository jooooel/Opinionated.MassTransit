using Consumer;
using Consumer.Contracts;
using Opinionated.MassTransit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Producer.Contracts;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json");

builder.Services
    .AddMassTransit(builder.Configuration)
    .AddEventConsumer<ISomethingHappenedEvent, SomethingHappenedEventConsumer>()
    .AddCommandConsumer<IDoSomethingCommand, DoSomethingCommandConsumer>()
    .Configure();

using var host = builder.Build();

await host.RunAsync();