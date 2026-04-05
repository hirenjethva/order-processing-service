using MongoDB.Bson;
using MongoDB.Driver;
using OrderProcessingService.Application.Repositories;
using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Infrastructure.Repositories;

public sealed class MongoOrderRepository : IOrderRepository
{
    private readonly IMongoCollection<Order> _collection;

    public MongoOrderRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Order>("orders");
    }

    public async Task<Order?> GetByIdAsync(string id) =>
        await _collection.Find(o => o.Id == id).FirstOrDefaultAsync();

    public async Task<Order> CreateAsync(Order order)
    {
        if (string.IsNullOrEmpty(order.Id))
            order.Id = ObjectId.GenerateNewId().ToString();

        var now = DateTime.UtcNow;
        order.CreatedAt = now;
        order.UpdatedAt = now;

        await _collection.InsertOneAsync(order);
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(o => o.Id == order.Id, order);
        return order;
    }
}
