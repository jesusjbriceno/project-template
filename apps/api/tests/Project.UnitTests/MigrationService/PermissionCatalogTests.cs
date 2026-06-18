using System.Text.RegularExpressions;
using Project.MigrationService;

namespace Project.UnitTests.MigrationService;

public class PermissionCatalogTests
{
    [Fact]
    public void All_ForcesInitialization_AndHasValidUniqueKeys()
    {
        var catalog = PermissionCatalog.All;

        Assert.Equal(22, catalog.Count);

        var keys = catalog.Select(entry => entry.Key.Value).ToArray();

        Assert.All(keys, key => Assert.Matches(KeyPattern, key));
        Assert.Equal(keys.Length, keys.Distinct().Count());
    }

    private static readonly Regex KeyPattern = new(
        @"^[a-z][a-z0-9]*\.[a-z][a-z0-9]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
}
