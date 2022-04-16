using MongoConcurrency.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoConcurrency.Data;

public class OptimisticConcurrencyDataAccessor : IDataAccessor
{
    public const string DbName = "CounterDb";
    public const string CollectionName = "CounterCollection";
    private readonly IMongoCollection<Counter> _collection;

    public OptimisticConcurrencyDataAccessor(string mongoConnectionString)
    {
        var client = new MongoClient(mongoConnectionString);
        _collection = client.GetDatabase("CounterDb").GetCollection<Counter>("CounterCollection");
    }

    public async Task<Counter> GetCounter(Guid id)
    {
        return await _collection.AsQueryable().Where(x => x.Id == id).SingleAsync();
    }

    public Task<ReplaceOneResult> ReplaceCounter(Guid id, Counter document)
    {
        throw new NotImplementedException();
    }
}