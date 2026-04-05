namespace OrderProcessingService.Application.Caching;

/// <summary>Keys and patterns used for product caching; keep in sync with readers.</summary>
public static class ProductCacheKeys
{
    public static string Product(string productId) => $"product:{productId}";

    /// <summary>Pattern covering list or bulk product cache entries.</summary>
    public const string ProductsPattern = "products:*";
}
