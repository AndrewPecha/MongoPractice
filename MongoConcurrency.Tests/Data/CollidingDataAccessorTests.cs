using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Mongo2Go;
using MongoConcurrency.Data;
using MongoConcurrency.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xunit;
using Xunit.Abstractions;

namespace MongoConcurrency.Tests.Data;

public class CollidingDataAccessorTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CollidingDataAccessorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task GetCounter_GetsExpectedCounter()
    {
        //Arrange
        var runner = MongoDbRunner.Start();
        var collection = await SetupCollection(runner);
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
        var collection = await SetupCollection(runner);
        var counter = new Counter
        {
            Id = Guid.NewGuid(),
            Value = 0
        };
        await collection.InsertOneAsync(counter);

        var dataAccessor = new CollidingDataAccessor(runner.ConnectionString);
        var document = await dataAccessor.GetCounter(counter.Id);

        //Act
        _testOutputHelper.WriteLine($"Before  : {document.Value}");

        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            var loaded = await dataAccessor.GetCounter(counter.Id);

            loaded.Value++;

            long result;
            do
            {
                result = (await dataAccessor.ReplaceCounter(counter.Id, loaded)).ModifiedCount;
            } while (result == 0);//ensure a document gets modified

            return result;
        }).ToList();

        var total = await Task.WhenAll(tasks);

        document = await dataAccessor.GetCounter(counter.Id);;

        _testOutputHelper.WriteLine($"After   : {document.Value}");
        _testOutputHelper.WriteLine($"Modified: {total.Sum(r => r)}");

        //Assert
    }

    private async Task<IMongoCollection<Counter>> SetupCollection(MongoDbRunner runner)
    {
        return new MongoClient(runner.ConnectionString)
            .GetDatabase(CollidingDataAccessor.DbName)
            .GetCollection<Counter>(CollidingDataAccessor.CollectionName);
    }
}