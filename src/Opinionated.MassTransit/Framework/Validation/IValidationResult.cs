using System.Collections.Generic;

namespace Opinionated.MassTransit.Framework.Validation;

public interface IValidationResult
{
    ValidationStatusCode StatusCode { get; }

    IEnumerable<ValidationError> ValidationErrors { get; }
}