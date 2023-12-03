using System.Threading.Tasks;

namespace MassTransit.Opinionated.Framework.Commands;

public interface ICommandConsumer<in TCommand> : IConsumer<TCommand> where TCommand : class, ICommand
{
    Task ExecuteAsync(TCommand command);
}