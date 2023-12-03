using System.Collections.Generic;

namespace MassTransit.Opinionated.Framework.Validation;

public class ValidationResult : IValidationResult
{
    public ValidationResult()
    {
    }

    public ValidationResult(ValidationStatusCode statusCode)
    {
        StatusCode = statusCode;
    }

    public ValidationResult(ValidationStatusCode statusCode, IEnumerable<ValidationError> validationErrors)
    {
        StatusCode = statusCode;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Status code of message validation.
    /// </summary>
    /// <para>If status code is such that indicates that the validation has failed, the <see cref="ValidationErrors"/>
    /// field contains a list of validation errors.</para>
    /// <seealso cref="ValidationStatusCode"/>
    public ValidationStatusCode StatusCode { get; set; }

    /// <summary>
    /// Collection of message validation errors.
    /// </summary>
    /// <seealso cref="ValidationError"/>
    public IEnumerable<ValidationError> ValidationErrors { get; set; }
}