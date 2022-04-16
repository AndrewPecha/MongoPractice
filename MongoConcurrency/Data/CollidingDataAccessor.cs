using MongoConcurrency.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoConcurrency.Data;

public class CollidingDataAccessor : IDataAccessor
{
    public const string DbName = "CounterDb";
    public const string CollectionName = "CounterCollection";
    private readonly IMongoCollection<Counter> _collection;

    public CollidingDataAccessor(string mongoConnectionString)
    {
        var client = new MongoClient(mongoConnectionString);
        _collection = client.GetDatabase(DbName).GetCollection<Counter>(CollectionName);
    }


    public async Task<Counter> GetCounter(Guid id)
    {
        return await _collection.AsQueryable().Where(x => x.Id == id).SingleAsync();
    }

    public async Task<ReplaceOneResult> ReplaceCounter(Guid id, Counter document)
    {
        
        
        return await _collection.ReplaceOneAsync(c => c.Id == id, document,
            new ReplaceOptions {IsUpsert = false});
    }
}