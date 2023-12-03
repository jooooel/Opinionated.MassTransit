using System.Collections.Generic;
using System.Threading.Tasks;

namespace MassTransit.Opinionated.Framework.Events;

public interface IBatchEventConsumer<in TEvent> : IConsumer<Batch<TEvent>>
    where TEvent : class, IEvent
{
    Task ExecuteManyAsync(IEnumerable<TEvent> events);
}