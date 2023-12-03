using Producer.Contracts;

namespace Producer;

public class SomethingHappenedEvent : ISomethingHappenedEvent
{
    public string? WhatHappened { get; set; }
}