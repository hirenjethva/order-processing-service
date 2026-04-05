namespace OrderProcessingService.Application.Caching;

/// <summary>Keys and patterns used for product caching; keep in sync with readers.</summary>
public static class ProductCacheKeys
{
    public const string AllProducts = "products:all";

    public static string Product(string productId) => $"products:{productId}";

    /// <summary>Pattern covering list and per-product cache entries.</summary>
    public const string ProductsPattern = "products:*";
}
