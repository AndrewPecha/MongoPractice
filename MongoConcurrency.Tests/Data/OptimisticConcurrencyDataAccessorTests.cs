using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Mongo2Go;
using MongoConcurrency.Data;
using MongoConcurrency.Data.Models;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace MongoConcurrency.Tests.Data;

public class OptimisticConcurrencyDataAccessorTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OptimisticConcurrencyDataAccessorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

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
    
    [Fact]
    public async Task TestCollision()
    {
        var runner = MongoDbRunner.Start();
        var collection = SetupCollection(runner);
        var counter = new Counter
        {
            Id = Guid.NewGuid(),
            Value = 0
        };
        await collection.InsertOneAsync(counter);

        var dataAccessor = new OptimisticConcurrencyDataAccessor(runner.ConnectionString);
        var document = await dataAccessor.GetCounter(counter.Id);

        //Act
        _testOutputHelper.WriteLine($"Before  : {document.Value}");

        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            long result;
            do
            {
                var existingDoc = await dataAccessor.GetCounter(counter.Id);

                var previousVersion = existingDoc.Version;
            
                existingDoc.Value++;
                existingDoc.Version++;
                
                result = (await dataAccessor.ReplaceCounter(counter.Id, previousVersion, existingDoc)).ModifiedCount;
            } while (result == 0);//ensure a document gets modified

            return result;
        }).ToList();

        var total = await Task.WhenAll(tasks);

        document = await dataAccessor.GetCounter(counter.Id);;

        _testOutputHelper.WriteLine($"After   : {document.Value}");
        _testOutputHelper.WriteLine($"Modified: {total.Sum(r => r)}");

        //Assert
    }

    private IMongoCollection<Counter> SetupCollection(MongoDbRunner runner)
    {
        return new MongoClient(runner.ConnectionString)
            .GetDatabase(CollidingDataAccessor.DbName)
            .GetCollection<Counter>(CollidingDataAccessor.CollectionName);
    }
}