using System;
using Consumer.Contracts;
using MassTransit;
using MassTransit.Opinionated.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Producer;
using Producer.Contracts;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json");

builder.Services
    .AddMassTransit(builder.Configuration)
    .MapEndpointConvention<IDoSomethingCommand>()
    .Configure();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var bus = services.GetRequiredService<IBus>();

    await bus.Publish<ISomethingHappenedEvent>(new SomethingHappenedEvent
    {
        WhatHappened = "Something happened over in the Producer"
    });
    Console.WriteLine("Published an event");

    await bus.Send<IDoSomethingCommand>(new DoSomethingCommand
    {
        DoThis = "I command you to take out the trash"
    });
    Console.WriteLine("Sent a command");
}
catch (Exception exception)
{
    Console.WriteLine(exception.Message);
}
