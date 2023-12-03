using System.Collections.Generic;
using System.Threading.Tasks;

namespace MassTransit.Opinionated.Framework.Commands;

public interface IBatchCommandConsumer<in TCommand> : IConsumer<Batch<TCommand>>
    where TCommand : class
{
    Task ExecuteManyAsync(IEnumerable<TCommand> commands);
}