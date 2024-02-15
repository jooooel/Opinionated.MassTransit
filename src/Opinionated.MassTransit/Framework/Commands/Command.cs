using System;

namespace Opinionated.MassTransit.Framework.Commands;

public abstract class Command : ICommand
{
    public Guid Id => Guid.NewGuid();
}