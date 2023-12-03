using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MassTransit.Opinionated.Framework.Events;

public abstract class BaseBatchEventConsumer<TEvent> : IBatchEventConsumer<TEvent>
    where TEvent : class, IEvent
{
    protected ConsumeContext<Batch<TEvent>> Context;

    public async Task Consume(ConsumeContext<Batch<TEvent>> context)
    {
        Context = context;
        var events = context.Message.Select(c => c.Message).ToList();
        await ExecuteManyAsync(events);
    }

    public abstract Task ExecuteManyAsync(IEnumerable<TEvent> events);
}