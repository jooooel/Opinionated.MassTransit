using System.Threading.Tasks;
using MassTransit;

namespace Opinionated.MassTransit.Framework.Commands;

public interface ICommandConsumer<in TCommand> : IConsumer<TCommand> where TCommand : class, ICommand
{
    Task ExecuteAsync(TCommand command);
}