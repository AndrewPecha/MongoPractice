using MongoConcurrency.Data.Models;

namespace MongoConcurrency.Data;

public interface IDataAccessor
{
    public Task<Counter> GetCounter(Guid id);
    public Task ReplaceCounter(Guid id, Counter document);
}