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
    
    public async Task<ReplaceOneResult> ReplaceCounter(Guid id, Counter newDocument)
    {
        throw new NotImplementedException();
    }
    
    public async Task<long> ReplaceCounterOptimisticConcurrency(Guid id, Action<Counter> updates)
    {
        long result;
        do
        {
            var existingDocument = await GetCounter(id);
            updates?.Invoke(existingDocument);
            
            var previousVersion = existingDocument.Version;
            existingDocument.Version++;
            
            result = (await _collection.ReplaceOneAsync(c => c.Id == id && c.Version == previousVersion, existingDocument,
                new ReplaceOptions {IsUpsert = false})).ModifiedCount;
        } while (result == 0);//ensure a document gets modified

        return result;
    }
}