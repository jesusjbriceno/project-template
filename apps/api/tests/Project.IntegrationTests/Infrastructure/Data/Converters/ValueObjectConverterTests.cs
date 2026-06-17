using Project.Domain.Policies;
using Project.Domain.ValueObjects;
using Project.Infrastructure.Data.Converters;

namespace Project.IntegrationTests.Infrastructure.Data.Converters;

/// <summary>
/// Unit tests for value-object converters (Email, PermissionKey, DeletionPolicy).
/// Verifies round-trip conversion: domain → store → domain.
/// </summary>
public sealed class ValueObjectConverterTests
{
    [Fact]
    public void Email_RoundTrip_NormalizesAndPreservesValue()
    {
        var original = Email.Create("User@Example.com");
        var converter = new EmailConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        // Email.Create normalizes to lowercase; round-trip preserves that
        var restoredEmail = (Email)restored!;
        Assert.Equal("user@example.com", restoredEmail.Value);
    }

    [Fact]
    public void PermissionKey_RoundTrip_Produces_SameValue()
    {
        var original = PermissionKey.Create("users.read");
        var converter = new PermissionKeyConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void Email_RoundTrip_TrimsAndLowercasesWhitespacePaddedInput()
    {
        var original = Email.Create("  JOHN@DOMAIN.COM  ");
        var converter = new EmailConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        var restoredEmail = (Email)restored!;
        Assert.Equal("john@domain.com", restoredEmail.Value);
    }

    [Fact]
    public void PermissionKey_RoundTrip_DifferentSegments_Produces_SameValue()
    {
        var original = PermissionKey.Create("admin.create");
        var converter = new PermissionKeyConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void DeletionPolicy_RoundTrip_RoleDefault_PreservesThreeTrueBooleans()
    {
        var original = DeletionPolicy.RoleDefault;
        var converter = new DeletionPolicyConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.NotNull(restored);
        var policy = (DeletionPolicy)restored!;
        Assert.Equal(original.SoftDeleteEnabled, policy.SoftDeleteEnabled);
        Assert.Equal(original.RecycleBinVisible, policy.RecycleBinVisible);
        Assert.Equal(original.RestoreAllowed, policy.RestoreAllowed);
        Assert.Equal(original.HardDeleteAllowed, policy.HardDeleteAllowed);
    }

    [Fact]
    public void DeletionPolicy_RoundTrip_PreservesFourBooleans()
    {
        var original = DeletionPolicy.UserDefault;
        var converter = new DeletionPolicyConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.NotNull(restored);
        var policy = (DeletionPolicy)restored!;
        Assert.Equal(original.SoftDeleteEnabled, policy.SoftDeleteEnabled);
        Assert.Equal(original.RecycleBinVisible, policy.RecycleBinVisible);
        Assert.Equal(original.RestoreAllowed, policy.RestoreAllowed);
        Assert.Equal(original.HardDeleteAllowed, policy.HardDeleteAllowed);
    }
}
