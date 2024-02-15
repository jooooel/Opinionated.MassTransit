using MassTransit;
using Opinionated.MassTransit.Framework.Commands;

namespace Consumer.Contracts;

[EntityName("consumer/do-something")]
public interface IDoSomethingCommand : ICommand
{
    public string? DoThis { get; set; }
}