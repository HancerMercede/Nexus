using Nexus.Store.Contract;

namespace Nexus.Store;

public static class NexusCacheExtensions
{
    private const string CompanyTagPrefix = "company";
    private const string CompanyListTag = "companies:list";

    public static async Task InvalidateCompanyAsync(this INexusEngine engine, int companyId)
    {
        await engine.InvalidateByTagsAsync(new[]
        {
            $"{CompanyTagPrefix}:{companyId}",
            CompanyListTag
        });
    }

    public static async Task SetCompanyAsync<T>(this INexusEngine engine, int companyId, T company)
    {
        await engine.SetAsync($"{CompanyTagPrefix}:{companyId}", company, new[]
        {
            $"{CompanyTagPrefix}:{companyId}",
            CompanyListTag
        });
    }

    public static async Task<T?> GetCompanyAsync<T>(this INexusEngine engine, int companyId)
    {
        return await engine.GetAsync<T>($"{CompanyTagPrefix}:{companyId}");
    }

    public static async Task InvalidateCompanyListAsync(this INexusEngine engine)
    {
        await engine.InvalidateByTagAsync(CompanyListTag);
    }

    public static async Task SetCompanyListAsync<T>(this INexusEngine engine, T companies)
    {
        await engine.SetAsync(CompanyListTag, companies, new[] { CompanyListTag });
    }

    public static async Task<T?> GetCompanyListAsync<T>(this INexusEngine engine)
    {
        return await engine.GetAsync<T>(CompanyListTag);
    }
}
