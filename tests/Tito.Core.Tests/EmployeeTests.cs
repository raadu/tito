using Tito.Core.Common;
using Tito.Core.Employees;

namespace Tito.Core.Tests;

public class EmployeeTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_normalizes_code_and_name()
    {
        // Arrange + Act
        var employee = new Employee("  e-01 ", "  Abdul    Basit ", Now);

        // Assert
        Assert.Equal("E-01", employee.EmployeeCode);
        Assert.Equal("Abdul Basit", employee.FullName);
    }

    [Theory]
    [InlineData("abdul basit")]
    [InlineData("  ABDUL   BASIT  ")]
    public void HasName_ignores_case_and_spacing(string typed)
    {
        var employee = new Employee("E-01", "Abdul Basit", Now);

        Assert.True(employee.HasName(typed));
    }

    [Fact]
    public void HasName_is_false_for_a_different_person()
    {
        var employee = new Employee("E-01", "Abdul Basit", Now);

        Assert.False(employee.HasName("Someone Else"));
    }

    [Theory]
    [InlineData("", "Abdul")]                    // missing code
    [InlineData("E-01", "   ")]                 // missing name
    [InlineData("E-0123456789012345678", "J")]  // code longer than 20
    public void Constructor_rejects_invalid_input(string code, string name)
    {
        var ex = Assert.Throws<DomainException>(() => new Employee(code, name, Now));

        Assert.Equal(DomainError.Validation, ex.Error);
    }
}
