using System;

namespace MassTransit.Opinionated.Framework.Commands;

public abstract class Command : ICommand
{
    public Guid Id => Guid.NewGuid();
}