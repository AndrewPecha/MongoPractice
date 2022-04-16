using MongoConcurrency.Data.Models;
using MongoDB.Driver;

namespace MongoConcurrency.Data;

public interface IDataAccessor
{
    public Task<Counter> GetCounter(Guid id);
    public Task<ReplaceOneResult> ReplaceCounter(Guid id, Counter document);
}