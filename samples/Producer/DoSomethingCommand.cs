using System;
using Consumer.Contracts;

namespace Producer;

public class DoSomethingCommand : IDoSomethingCommand
{
    public Guid Id { get; }

    public string? DoThis { get; set; }
}