using MassTransit;
using MassTransit.Opinionated.Framework.Events;

namespace Producer.Contracts;

[EntityName("something-happened")]
public interface ISomethingHappenedEvent : IEvent
{
    public string? WhatHappened { get; set; }
}