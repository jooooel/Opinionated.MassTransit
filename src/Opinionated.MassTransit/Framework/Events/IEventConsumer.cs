using System.Threading.Tasks;
using MassTransit;

namespace Opinionated.MassTransit.Framework.Events;

public interface IEventConsumer<in TEvent> : IConsumer<TEvent>
    where TEvent : class, IEvent
{
    Task ExecuteAsync(TEvent @event);
}