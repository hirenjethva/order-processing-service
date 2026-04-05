using MongoDB.Driver;
using OrderProcessingService.Application.Repositories;
using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Infrastructure.Repositories;

public sealed class MongoProductRepository : IProductRepository
{
    private readonly IMongoCollection<Product> _collection;

    public MongoProductRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Product>("products");
    }

    public async Task<List<Product>> GetAllAsync() =>
        await _collection.Find(FilterDefinition<Product>.Empty).ToListAsync();

    public async Task<Product?> GetByIdAsync(string id) =>
        await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<Product?> ReserveStockAsync(string productId, int requestedQuantity)
    {
        if (requestedQuantity <= 0)
            return null;

        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, productId),
            Builders<Product>.Filter.Gte(p => p.StockQuantity, requestedQuantity));

        var update = Builders<Product>.Update.Inc(p => p.StockQuantity, -requestedQuantity);

        var options = new FindOneAndUpdateOptions<Product>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _collection.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task SeedProductsAsync()
    {
        if (await _collection.CountDocumentsAsync(FilterDefinition<Product>.Empty) > 0)
            return;

        var now = DateTime.UtcNow;
        var products = new List<Product>
        {
            new()
            {
                Id = "prod-001",
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with USB receiver.",
                Price = 29.99m,
                StockQuantity = 100,
                CreatedAt = now
            },
            new()
            {
                Id = "prod-002",
                Name = "Mechanical Keyboard",
                Description = "Tenkeyless RGB mechanical keyboard.",
                Price = 119.50m,
                StockQuantity = 45,
                CreatedAt = now
            },
            new()
            {
                Id = "prod-003",
                Name = "USB-C Hub",
                Description = "7-in-1 USB-C hub with HDMI and SD card reader.",
                Price = 49.00m,
                StockQuantity = 200,
                CreatedAt = now
            },
            new()
            {
                Id = "prod-004",
                Name = "Noise-Cancelling Headphones",
                Description = "Over-ear Bluetooth headphones with ANC.",
                Price = 199.99m,
                StockQuantity = 30,
                CreatedAt = now
            },
            new()
            {
                Id = "prod-005",
                Name = "Webcam 1080p",
                Description = "Full HD webcam with built-in microphone.",
                Price = 79.00m,
                StockQuantity = 60,
                CreatedAt = now
            }
        };

        await _collection.InsertManyAsync(products);
    }
}
