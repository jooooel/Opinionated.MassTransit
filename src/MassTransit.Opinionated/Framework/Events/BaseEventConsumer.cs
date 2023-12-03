using System.Threading.Tasks;

namespace MassTransit.Opinionated.Framework.Events;

public abstract class BaseEventConsumer<TEvent> : IEventConsumer<TEvent>
    where TEvent : class, IEvent
{
    protected ConsumeContext<TEvent> Context;

    public async Task Consume(ConsumeContext<TEvent> context)
    {
        Context = context;
        var @event = context.Message;
        await ExecuteAsync(@event);
    }

    public abstract Task ExecuteAsync(TEvent @event);
}