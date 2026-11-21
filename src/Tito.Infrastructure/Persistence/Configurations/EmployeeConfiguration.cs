using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tito.Core.Employees;

namespace Tito.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);                       // PRIMARY KEY, IDENTITY by convention

        builder.Property(e => e.EmployeeCode)
            .HasMaxLength(Employee.CodeMaxLength)        // nvarchar(20) - reuses the Core constant
            .IsRequired();                               // NOT NULL
        builder.HasIndex(e => e.EmployeeCode).IsUnique(); // no two employees share a code

        builder.Property(e => e.FullName)
            .HasMaxLength(Employee.NameMaxLength)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc).IsRequired();
    }
}
