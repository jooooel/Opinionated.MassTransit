using System;

namespace MassTransit.Opinionated.Framework.Commands;

[ExcludeFromTopology]
public interface ICommand
{
    Guid Id { get; }
}