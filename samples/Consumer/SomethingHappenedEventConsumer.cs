using Opinionated.MassTransit.Configuration;
using Opinionated.MassTransit.Framework.Events;
using Producer.Contracts;

namespace Consumer;

[SubscriptionName("consumer-something-happened")]
public class SomethingHappenedEventConsumer : BaseEventConsumer<ISomethingHappenedEvent>
{
    public override Task ExecuteAsync(ISomethingHappenedEvent @event)
    {
        Console.WriteLine($"Received event: {@event.WhatHappened}");
        return Task.CompletedTask;
    }
}