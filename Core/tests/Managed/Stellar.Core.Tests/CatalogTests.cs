using Stellar.Core.Data.Catalog;
using Stellar.Core.Services;
using Stellar.Kernel.Services;

namespace Stellar.Core.Tests;

public class CatalogTests
{
    private sealed class Names()
        : Catalog<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        protected override string NormalizeKey(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return key.Length == 0 ? throw new ArgumentException("No name", nameof(key)) : key.Trim();
        }

        protected override Exception NotFound(string key)
        {
            return new InvalidOperationException($"No {key}");
        }
    }

    [Fact]
    public void Values_AreFoundByKey()
    {
        var catalog = new Catalog<string, int>();

        catalog.TryAdd("one", 1).Should().BeTrue();
        catalog.TryAdd("one", 2).Should().BeFalse();
        catalog.Get("one").Should().Be(1);

        catalog.Set("one", 3);
        catalog.Get("one").Should().Be(3);
        catalog.Count.Should().Be(1);

        catalog.Remove("one").Should().BeTrue();
        catalog.Contains("one").Should().BeFalse();
        catalog.Invoking(c => c.Get("one")).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ACatalog_BringsKeysToItsForm_AndSaysWhatIsMissing()
    {
        var catalog = new Names();
        catalog.Set(" Player ", 1);

        catalog.Get("PLAYER").Should().Be(1);
        catalog.Keys.Should().Equal("Player");
        catalog.Contains("").Should().BeFalse();
        catalog.Invoking(c => c.Set("", 2)).Should().Throw<ArgumentException>();
        catalog.Invoking(c => c.Get("enemy")).Should().Throw<InvalidOperationException>().WithMessage("No enemy");
    }

    [Fact]
    public void ACatalog_IsASnapshotWhenListed()
    {
        var catalog = new Catalog<int, string> { };
        catalog.Set(1, "a");
        catalog.Set(2, "b");

        foreach (var (key, _) in catalog) catalog.Remove(key);

        catalog.Should().BeEmpty();
    }

    [Fact]
    public void Services_FulfilTheirContract()
    {
        IServiceCatalog services = new ServiceCatalog();

        services.Invoking(s => s.Set(typeof(IDisposable), "not disposable")).Should().Throw<ArgumentException>();
        services.Set<IComparable>("text");
        services.Find<IComparable>().Should().Be("text");
        services.Get(typeof(IComparable)).Should().Be("text");
    }
}
