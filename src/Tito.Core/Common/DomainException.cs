namespace Tito.Core.Common;

/// <summary>
/// Thrown when a business rule is violated. The API layer maps it to an HTTP error.
/// </summary>
public sealed class DomainException(DomainError error, string message) : Exception(message)
{
    public DomainError Error { get; } = error;
}

/// <summary>Kinds of rule violations; the API maps each to a status code.</summary>
public enum DomainError
{
    Validation,   // bad input            -> 400
    Conflict,     // clashes with data    -> 409
    NotFound,     // missing record       -> 404
}
