using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Opinionated.MassTransit.Framework.Validation;

namespace Opinionated.MassTransit.Framework.Commands;

public abstract class BaseCommandConsumer<TCommand> : ICommandConsumer<TCommand> where TCommand : class, ICommand
{
    protected ConsumeContext<TCommand> Context;

    public async Task Consume(ConsumeContext<TCommand> context)
    {
        Context = context;

        var command = Context.Message;

        var validationResult = await ValidateAsync(command, context.CancellationToken);
        if (validationResult.StatusCode != ValidationStatusCode.ValidationFail)
        {
            await ExecuteAsync(command, context.CancellationToken);
        }
    }

    public abstract Task ExecuteAsync(TCommand command, CancellationToken cancellationToken = default);

    protected virtual async Task<Opinionated.MassTransit.Framework.Validation.ValidationResult> ValidateAsync(TCommand command, CancellationToken cancellationToken = default) =>
        await Task.FromResult(ValidationSuccess());

    private static Opinionated.MassTransit.Framework.Validation.ValidationResult ValidationSuccess() => new(ValidationStatusCode.ValidationSuccess);
}