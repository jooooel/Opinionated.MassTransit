using System;
using MassTransit;

namespace Opinionated.MassTransit.Framework.Commands;

[ExcludeFromTopology]
public interface ICommand
{
    Guid Id { get; }
}