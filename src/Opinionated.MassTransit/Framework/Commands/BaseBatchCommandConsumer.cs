using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;

namespace Opinionated.MassTransit.Framework.Commands;

public abstract class BaseBatchCommandConsumer<TCommand> : IBatchCommandConsumer<TCommand>
    where TCommand : class, ICommand
{
    protected ConsumeContext<Batch<TCommand>> Context;

    public async Task Consume(ConsumeContext<Batch<TCommand>> context)
    {
        Context = context;

        var commands = context.Message.Select(c => c.Message).ToList();

        await ExecuteManyAsync(commands);
    }

    public abstract Task ExecuteManyAsync(IEnumerable<TCommand> command);
}