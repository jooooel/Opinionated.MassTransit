using System.Threading.Tasks;
using MassTransit.Opinionated.Framework.Validation;

namespace MassTransit.Opinionated.Framework.Commands;

public abstract class BaseCommandConsumer<TCommand> : ICommandConsumer<TCommand> where TCommand : class, ICommand
{
    protected ConsumeContext<TCommand> Context;

    public async Task Consume(ConsumeContext<TCommand> context)
    {
        Context = context;

        var command = Context.Message;

        var validationResult = await ValidateAsync(command);
        if (validationResult.StatusCode != ValidationStatusCode.ValidationFail)
        {
            await ExecuteAsync(command);
        }
    }

    public abstract Task ExecuteAsync(TCommand command);

    protected virtual async Task<MassTransit.Opinionated.Framework.Validation.ValidationResult> ValidateAsync(TCommand command) =>
        await Task.FromResult(ValidationSuccess());

    private static MassTransit.Opinionated.Framework.Validation.ValidationResult ValidationSuccess() => new(ValidationStatusCode.ValidationSuccess);
}