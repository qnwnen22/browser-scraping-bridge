using BrowserScrapingBridge.Server.Sourcing;

namespace BrowserScrapingBridge.Server.Services;

public sealed record SiteCollectorInfo(string Id, string Site, string Name, string Implementation,
    string Language, string Currency, bool SupportsParsing, string[] RequiredSettings);

public sealed class SiteCollectorCatalog
{
    private readonly Dictionary<string, Type> _types;
    public IReadOnlyList<SiteCollectorInfo> Sites { get; }

    public SiteCollectorCatalog()
    {
        _types = typeof(ICollect).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ICollect).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
            .ToDictionary(type => type.FullName!["BrowserScrapingBridge.Server.Sourcing.".Length..], StringComparer.OrdinalIgnoreCase);
        Sites = _types.Select(entry =>
        {
            var collector = (ICollect)Activator.CreateInstance(entry.Value)!;
            var settings = collector.Site switch
            {
                "Domeme" => new[] { "domemeApiKey (상품)" },
                "OwnerClan" => new[] { "ownerClanToken 또는 ownerClanAccount + ownerClanPassword" },
                "Taobao" => new[] { "taobaoApiToken (상품)" },
                _ => Array.Empty<string>()
            };
            return new SiteCollectorInfo(entry.Key, collector.Site, collector.Name, entry.Value.Name,
                collector.Language.ToString(), collector.Currency.ToString(), collector.SupportsCaching, settings);
        }).OrderBy(site => site.Name).ThenBy(site => site.Id).ToArray();
    }

    public ICollect Create(string site)
    {
        if (_types.TryGetValue(site, out var type)) return (ICollect)Activator.CreateInstance(type)!;
        // 기존 팩토리에 있던 사이트명/한글 별칭은 그대로 지원합니다.
        var preferred = CollectFactory.CreateInstance(site, null!, null!);
        if (preferred is not null) return preferred;
        var matches = Sites.Where(info => info.Site.Equals(site, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length == 1) return (ICollect)Activator.CreateInstance(_types[matches[0].Id])!;
        throw new ArgumentException(matches.Length > 1
            ? "동일한 사이트의 파서가 여러 개 있습니다. /api/sourcing/sites의 id를 지정해주세요."
            : "지원하지 않는 사이트입니다. /api/sourcing/sites에서 id를 확인해주세요.");
    }
}
