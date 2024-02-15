using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;

namespace Opinionated.MassTransit.Framework.Events;

public interface IBatchEventConsumer<in TEvent> : IConsumer<Batch<TEvent>>
    where TEvent : class, IEvent
{
    Task ExecuteManyAsync(IEnumerable<TEvent> events);
}