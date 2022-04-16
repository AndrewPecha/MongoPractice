using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mongo2Go;
using MongoConcurrency.Data;
using MongoConcurrency.Data.Models;
using MongoDB.Driver;
using Xunit;

namespace MongoConcurrency.Tests.Data;

public class OptimisticConcurrencyDataAccessorTests
{
    [Fact]
    public async Task GetCounter_GetsExpectedCounter()
    {
        //Arrange
        var runner = MongoDbRunner.Start();
        var collection = new MongoClient(runner.ConnectionString).GetDatabase(OptimisticConcurrencyDataAccessor.DbName).GetCollection<Counter>(OptimisticConcurrencyDataAccessor.CollectionName);
        var expected = new Counter
        {
            Id = Guid.NewGuid(),
            Value = 0
        };
        await collection.InsertOneAsync(expected);

        var dataAccessor = new CollidingDataAccessor(runner.ConnectionString);
        
        //Act
        var actual = await dataAccessor.GetCounter(expected.Id);

        //Assert
        actual.Should().BeEquivalentTo(expected);
    }
}