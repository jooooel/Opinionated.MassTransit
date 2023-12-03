using System.Threading.Tasks;

namespace MassTransit.Opinionated.Framework.Events;

public interface IEventConsumer<in TEvent> : IConsumer<TEvent>
    where TEvent : class, IEvent
{
    Task ExecuteAsync(TEvent @event);
}