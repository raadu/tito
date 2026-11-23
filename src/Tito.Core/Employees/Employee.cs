using Tito.Core.Common;

namespace Tito.Core.Employees;

/// <summary>A person who records attendance. Identified by the code they type in.</summary>
public sealed class Employee
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 100;

    // EF Core needs a parameterless constructor to build objects from database rows.
    // `private` hides it from our own code so nobody creates an invalid Employee.
    private Employee() { }

    public Employee(string employeeCode, string fullName, DateTime createdAtUtc)
    {
        EmployeeCode = NormalizeCode(employeeCode);
        FullName = NormalizeName(fullName);
        CreatedAtUtc = createdAtUtc;
    }

    public int Id { get; private set; }                       // database primary key
    public string EmployeeCode { get; private set; } = null!; // `null!` = "EF fills this in, trust me"
    public string FullName { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>True if the typed name matches ours, ignoring case and extra spaces.</summary>
    public bool HasName(string fullName) =>
        string.Equals(NormalizeName(fullName), FullName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Codes are compared uppercase and trimmed: " e-01 " -> "E-01".</summary>
    public static string NormalizeCode(string? code)
    {
        var value = code?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(value))
            throw new DomainException(DomainError.Validation, "Employee ID is required.");
        if (value.Length > CodeMaxLength)
            throw new DomainException(DomainError.Validation, $"Employee ID must be at most {CodeMaxLength} characters.");
        return value;
    }

    /// <summary>Trims and collapses inner whitespace: "  Abdul   Basit " -> "Abdul Basit".</summary>
    public static string NormalizeName(string? name)
    {
        var parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var value = string.Join(' ', parts);
        if (value.Length == 0)
            throw new DomainException(DomainError.Validation, "Full name is required.");
        if (value.Length > NameMaxLength)
            throw new DomainException(DomainError.Validation, $"Full name must be at most {NameMaxLength} characters.");
        return value;
    }
}
