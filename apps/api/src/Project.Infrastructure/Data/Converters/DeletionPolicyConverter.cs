using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.Policies;

namespace Project.Infrastructure.Data.Converters;

/// <summary>
/// Converts DeletionPolicy to/from JSONB using System.Text.Json.
/// </summary>
public sealed class DeletionPolicyConverter : ValueConverter<DeletionPolicy, string>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public DeletionPolicyConverter()
        : base(
            policy => JsonSerializer.Serialize(policy, Options),
            json => JsonSerializer.Deserialize<DeletionPolicy>(json, Options)
                ?? DeletionPolicy.UserDefault)
    {
    }
}
