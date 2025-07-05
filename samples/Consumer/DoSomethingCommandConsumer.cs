using System.Threading;
using Consumer.Contracts;
using Opinionated.MassTransit.Framework.Commands;

namespace Consumer;

public class DoSomethingCommandConsumer : BaseCommandConsumer<IDoSomethingCommand>
{
    public override Task ExecuteAsync(IDoSomethingCommand command, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Received command: {command.DoThis}");
        return Task.CompletedTask;
    }
}