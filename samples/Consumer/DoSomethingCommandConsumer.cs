using Consumer.Contracts;
using Opinionated.MassTransit.Framework.Commands;

namespace Consumer;

public class DoSomethingCommandConsumer : BaseCommandConsumer<IDoSomethingCommand>
{
    public override Task ExecuteAsync(IDoSomethingCommand command)
    {
        Console.WriteLine($"Received command: {command.DoThis}");
        return Task.CompletedTask;
    }
}