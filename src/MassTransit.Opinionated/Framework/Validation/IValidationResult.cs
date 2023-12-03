using System.Collections.Generic;

namespace MassTransit.Opinionated.Framework.Validation;

public interface IValidationResult
{
    ValidationStatusCode StatusCode { get; }

    IEnumerable<ValidationError> ValidationErrors { get; }
}