using MassTransit;
using MassTransit.Opinionated.Framework.Commands;

namespace Consumer.Contracts;

[EntityName("consumer/do-something")]
public interface IDoSomethingCommand : ICommand
{
    public string? DoThis { get; set; }
}