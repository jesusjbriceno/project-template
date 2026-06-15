using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects;

namespace Project.Infrastructure.Data.Converters;

/// <summary>
/// Converts Email value object to/from its string Value.
/// Email.Create performs normalization (lowercase, trim).
/// </summary>
public sealed class EmailConverter : ValueConverter<Email, string>
{
    public EmailConverter()
        : base(email => email.Value, s => Email.Create(s))
    {
    }
}
