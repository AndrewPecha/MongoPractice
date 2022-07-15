using System.Reflection;
using MongoConcurrency.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Polly;

namespace MongoConcurrency.Data;

public class OptimisticConcurrencyDataAccessor
{
    public const string DbName = "CounterDb";
    public const string CollectionName = "CounterCollection";
    private readonly IMongoCollection<VersionTrackerClass> _collection;

    public OptimisticConcurrencyDataAccessor(string mongoConnectionString)
    {
        var client = new MongoClient(mongoConnectionString);
        _collection = client.GetDatabase("CounterDb").GetCollection<VersionTrackerClass>("CounterCollection");
    }

    public async Task<VersionTrackerClass> GetCounter(Guid id)
    {
        return await _collection.AsQueryable().Where(x => x.Id == id).SingleAsync();
    }

    public async Task<ReplaceOneResult> ReplaceCounter(Guid id, VersionTrackerClass newDocument)
    {
        long result;
        do
        {
            var existingDocument = await GetCounter(id);

            var previousVersion = existingDocument.Version;
            CopyPropertiesExceptVersion(newDocument, existingDocument);
            existingDocument.Version++;

            result = (await _collection.ReplaceOneAsync(c => c.Id == id && c.Version == previousVersion,
                existingDocument,
                new ReplaceOptions {IsUpsert = false})).ModifiedCount;
        } while (result == 0); //ensure a document gets modified

        return null;
    }

    public async Task UpdateCounter(Guid id, Action<VersionTrackerClass> updates)
    {
        long result;
        do
        {
            var existingDocument = await GetCounter(id);
            updates?.Invoke(existingDocument);

            var previousVersion = existingDocument.Version;
            existingDocument.Version++;

            result = (await _collection.ReplaceOneAsync(c => c.Id == id && c.Version == previousVersion,
                existingDocument,
                new ReplaceOptions {IsUpsert = false})).ModifiedCount;
        } while (result == 0); //ensure a document gets modified
    }

    public async Task<VersionTrackerClass> DoBusinessLogicToDocumentAndSaveWithOptimisticConcurrency(Guid id,
        Func<VersionTrackerClass, VersionTrackerClass> businessLogic)
    {
        long modifiedCounter;
        VersionTrackerClass result;
        var retryCounter = 0;

        do
        {
            var existingDocument = await GetCounter(id);
            result = businessLogic?.Invoke(existingDocument)!;

            var previousVersion = existingDocument.Version;
            existingDocument.Version++;

            modifiedCounter = (await _collection.ReplaceOneAsync(c => c.Id == id && c.Version == previousVersion,
                existingDocument,
                new ReplaceOptions {IsUpsert = false})).ModifiedCount;
            retryCounter++;
        } while (modifiedCounter == 0 && retryCounter < 5); //ensure a document gets modified

        return result!;
    }

    //modified from https://stackoverflow.com/a/33814017/8484685
    private void CopyPropertiesExceptVersion<T>(T sourceObject, T targetObject)
    {
        var properties = typeof(T).GetProperties().Where(p => p.CanWrite && p.Name != "Version");
        foreach (PropertyInfo property in properties)
        {
            property.SetValue(targetObject, property.GetValue(sourceObject, null), null);
        }
    }
}