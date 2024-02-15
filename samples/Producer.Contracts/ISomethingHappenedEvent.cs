using MassTransit;
using Opinionated.MassTransit.Framework.Events;

namespace Producer.Contracts;

[EntityName("something-happened")]
public interface ISomethingHappenedEvent : IEvent
{
    public string? WhatHappened { get; set; }
}