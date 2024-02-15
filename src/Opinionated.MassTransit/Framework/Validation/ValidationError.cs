namespace Opinionated.MassTransit.Framework.Validation;

public class ValidationError
{
    public ValidationError()
    {
    }

    public ValidationError(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; set; }
}