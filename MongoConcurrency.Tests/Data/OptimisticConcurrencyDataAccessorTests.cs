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
        var collection = new MongoClient(runner.ConnectionString).GetDatabase(OptimisticConcurrencyDataAccessor.DbName).GetCollection<VersionTrackerClass>(OptimisticConcurrencyDataAccessor.CollectionName);
        var expected = new VersionTrackerClass
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
        var counter = new VersionTrackerClass
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
            await dataAccessor.UpdateCounter(counter.Id, IncrementValue);

            return 1;
        }).ToList();

        var total = await Task.WhenAll(tasks);

        document = await dataAccessor.GetCounter(counter.Id);;

        _testOutputHelper.WriteLine($"After   : {document.Value}");
        _testOutputHelper.WriteLine($"Modified: {total.Sum(r => r)}");

        //Assert
    }

    private void IncrementValue(VersionTrackerClass versionTrackerClass)
    {
        versionTrackerClass.Value++;
    }
    
    [Fact]
    public async Task TestCollision_Replace()
    {
        //maybe a long running process with old data has old version while a new shorter lived process overwrites?
        //Arrange

        //Act

        //Assert
    }

    private IMongoCollection<VersionTrackerClass> SetupCollection(MongoDbRunner runner)
    {
        return new MongoClient(runner.ConnectionString)
            .GetDatabase(CollidingDataAccessor.DbName)
            .GetCollection<VersionTrackerClass>(CollidingDataAccessor.CollectionName);
    }
}