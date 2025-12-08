using Microsoft.Xna.Framework;
using StarSystemData.Models;
using StarSystemData.Runtime;
using Xunit;

namespace StarSystemData.Tests;

/// <summary>
/// Unit tests for the star system query system
/// </summary>
public class StarSystemQueryTests
{
    private static StarSystemDatabase CreateTestDatabase()
    {
        var database = new StarSystemDatabase();
        
        // Add sample systems similar to real EDSM data
        database.Add(new StarSystemRecord { Id = 27, Name = "Sol", X = 0, Y = 0, Z = 0, RequirePermit = true, PermitName = "Sol" });
        database.Add(new StarSystemRecord { Id = 28, Name = "Alpha Centauri", X = 3.03f, Y = -0.08f, Z = 3.15f });
        database.Add(new StarSystemRecord { Id = 29, Name = "Barnard's Star", X = -3.03f, Y = 1.37f, Z = 4.94f });
        database.Add(new StarSystemRecord { Id = 30, Name = "Wolf 359", X = 3.86f, Y = 6.47f, Z = 1.93f });
        database.Add(new StarSystemRecord { Id = 31, Name = "Lalande 21185", X = -6.54f, Y = 1.65f, Z = 4.93f });
        database.Add(new StarSystemRecord { Id = 32, Name = "Sirius", X = 6.22f, Y = -1.01f, Z = -1.79f });
        database.Add(new StarSystemRecord { Id = 33, Name = "Luyten 726-8", X = 5.82f, Y = -3.00f, Z = -0.67f });
        database.Add(new StarSystemRecord { Id = 34, Name = "Ross 154", X = 1.93f, Y = -0.94f, Z = -9.46f });
        database.Add(new StarSystemRecord { Id = 35, Name = "Ross 248", X = 7.45f, Y = -2.42f, Z = 7.19f });
        database.Add(new StarSystemRecord { Id = 36, Name = "Epsilon Eridani", X = 1.93f, Y = -7.76f, Z = -6.92f });
        
        return database;
    }
    
    private static ContentStarSystemProvider CreateTestProvider()
    {
        return new ContentStarSystemProvider(CreateTestDatabase());
    }
    
    [Fact]
    public void GetAllSystems_ReturnsAllSystems()
    {
        var provider = CreateTestProvider();
        
        var systems = provider.GetAllSystems();
        
        Assert.Equal(10, systems.Count);
    }
    
    [Fact]
    public void SystemCount_ReturnsCorrectCount()
    {
        var provider = CreateTestProvider();
        
        Assert.Equal(10, provider.SystemCount);
    }
    
    [Theory]
    [InlineData("Sol")]
    [InlineData("Alpha Centauri")]
    [InlineData("Barnard's Star")]
    [InlineData("Wolf 359")]
    [InlineData("Sirius")]
    public void GetSystemByName_ReturnsCorrectSystem(string systemName)
    {
        var provider = CreateTestProvider();
        
        var system = provider.GetSystemByName(systemName);
        
        Assert.NotNull(system);
        Assert.Equal(systemName, system.Name);
    }
    
    [Fact]
    public void GetSystemByName_CaseInsensitive()
    {
        var provider = CreateTestProvider();
        
        var system1 = provider.GetSystemByName("sol");
        var system2 = provider.GetSystemByName("SOL");
        var system3 = provider.GetSystemByName("Sol");
        
        Assert.NotNull(system1);
        Assert.NotNull(system2);
        Assert.NotNull(system3);
        Assert.Equal(system1.Id, system2.Id);
        Assert.Equal(system2.Id, system3.Id);
    }
    
    [Fact]
    public void GetSystemByName_ReturnsNullForUnknown()
    {
        var provider = CreateTestProvider();
        
        var system = provider.GetSystemByName("Unknown System 12345");
        
        Assert.Null(system);
    }
    
    [Fact]
    public void GetSystemByName_ReturnsNullForEmpty()
    {
        var provider = CreateTestProvider();
        
        Assert.Null(provider.GetSystemByName(""));
        Assert.Null(provider.GetSystemByName(null!));
    }
    
    [Theory]
    [InlineData(27, "Sol")]
    [InlineData(28, "Alpha Centauri")]
    [InlineData(32, "Sirius")]
    public void GetSystemById_ReturnsCorrectSystem(int id, string expectedName)
    {
        var provider = CreateTestProvider();
        
        var system = provider.GetSystemById(id);
        
        Assert.NotNull(system);
        Assert.Equal(expectedName, system.Name);
        Assert.Equal(id, system.Id);
    }
    
    [Fact]
    public void GetSystemById_ReturnsNullForUnknown()
    {
        var provider = CreateTestProvider();
        
        var system = provider.GetSystemById(99999);
        
        Assert.Null(system);
    }
    
    [Fact]
    public void GetSystemsInSphere_ByVector_ReturnsSystemsWithinRadius()
    {
        var provider = CreateTestProvider();
        var center = Vector3.Zero; // Sol's position
        
        var systems = provider.GetSystemsInSphere(center, 5f);
        
        Assert.NotEmpty(systems);
        Assert.Contains(systems, s => s.Name == "Sol");
        Assert.Contains(systems, s => s.Name == "Alpha Centauri");
        
        // All returned systems should be within radius
        foreach (var system in systems)
        {
            Assert.True(system.DistanceTo(center) <= 5f);
        }
    }
    
    [Fact]
    public void GetSystemsInSphere_ByName_ReturnsSystemsWithinRadius()
    {
        var provider = CreateTestProvider();
        
        var systems = provider.GetSystemsInSphere("Sol", 10f);
        
        Assert.NotEmpty(systems);
        Assert.Contains(systems, s => s.Name == "Sol");
        
        // Should include nearby systems
        var sol = provider.GetSystemByName("Sol");
        foreach (var system in systems)
        {
            Assert.True(sol!.DistanceTo(system) <= 10f);
        }
    }
    
    [Fact]
    public void GetSystemsInSphere_ByName_ReturnsEmptyForUnknownSystem()
    {
        var provider = CreateTestProvider();
        
        var systems = provider.GetSystemsInSphere("Unknown System", 100f);
        
        Assert.Empty(systems);
    }
    
    [Fact]
    public void GetSystemsInSphere_SmallRadius_ReturnsOnlyCenter()
    {
        var provider = CreateTestProvider();
        
        var systems = provider.GetSystemsInSphere("Sol", 0.1f);
        
        Assert.Single(systems);
        Assert.Equal("Sol", systems[0].Name);
    }
    
    [Fact]
    public void GetNearestSystems_ReturnsCorrectCount()
    {
        var provider = CreateTestProvider();
        var position = Vector3.Zero;
        
        var systems = provider.GetNearestSystems(position, 3);
        
        Assert.Equal(3, systems.Count);
    }
    
    [Fact]
    public void GetNearestSystems_ReturnsOrderedByDistance()
    {
        var provider = CreateTestProvider();
        var position = Vector3.Zero;
        
        var systems = provider.GetNearestSystems(position, 5);
        
        // First should be Sol (at origin)
        Assert.Equal("Sol", systems[0].Name);
        
        // Should be ordered by distance
        for (int i = 1; i < systems.Count; i++)
        {
            Assert.True(systems[i - 1].DistanceTo(position) <= systems[i].DistanceTo(position));
        }
    }
    
    [Fact]
    public void GetNearestSystems_RequestMoreThanAvailable_ReturnsAll()
    {
        var provider = CreateTestProvider();
        var position = Vector3.Zero;
        
        var systems = provider.GetNearestSystems(position, 100);
        
        Assert.Equal(10, systems.Count); // Only 10 systems in test data
    }
    
    [Theory]
    [InlineData("Sol", 1)]
    [InlineData("Ross", 2)] // Ross 154 and Ross 248
    [InlineData("Alpha", 1)]
    [InlineData("ar", 1)] // Barnard's Star (only system containing 'ar')
    public void SearchByName_FindsMatchingSystems(string searchTerm, int expectedCount)
    {
        var provider = CreateTestProvider();
        
        var systems = provider.SearchByName(searchTerm);
        
        Assert.Equal(expectedCount, systems.Count);
    }
    
    [Fact]
    public void SearchByName_CaseInsensitive()
    {
        var provider = CreateTestProvider();
        
        var lower = provider.SearchByName("sol");
        var upper = provider.SearchByName("SOL");
        
        Assert.Single(lower);
        Assert.Single(upper);
    }
    
    [Fact]
    public void SearchByName_ReturnsEmptyForNoMatch()
    {
        var provider = CreateTestProvider();
        
        var systems = provider.SearchByName("xyz12345");
        
        Assert.Empty(systems);
    }
    
    [Fact]
    public void SearchByName_RespectsMaxResults()
    {
        var provider = CreateTestProvider();
        
        // Search for 'a' which matches many systems
        var systems = provider.SearchByName("a", maxResults: 2);
        
        Assert.True(systems.Count <= 2);
    }
    
    [Fact]
    public void GetDistance_ReturnsCorrectDistance()
    {
        var provider = CreateTestProvider();
        
        var distance = provider.GetDistance("Sol", "Alpha Centauri");
        
        Assert.NotNull(distance);
        // Alpha Centauri is approximately 4.37 light years from Sol
        Assert.True(distance > 4f && distance < 5f);
    }
    
    [Fact]
    public void GetDistance_SameSystem_ReturnsZero()
    {
        var provider = CreateTestProvider();
        
        var distance = provider.GetDistance("Sol", "Sol");
        
        Assert.NotNull(distance);
        Assert.Equal(0f, distance);
    }
    
    [Fact]
    public void GetDistance_UnknownSystem_ReturnsNull()
    {
        var provider = CreateTestProvider();
        
        Assert.Null(provider.GetDistance("Sol", "Unknown"));
        Assert.Null(provider.GetDistance("Unknown", "Sol"));
        Assert.Null(provider.GetDistance("Unknown", "Unknown2"));
    }
    
    [Fact]
    public void GetRoute_ReturnsStartAndEnd()
    {
        var provider = CreateTestProvider();
        
        var route = provider.GetRoute("Sol", "Sirius");
        
        Assert.Equal(2, route.Count);
        Assert.Equal("Sol", route[0].Name);
        Assert.Equal("Sirius", route[1].Name);
    }
    
    [Fact]
    public void GetRoute_UnknownSystem_ReturnsEmpty()
    {
        var provider = CreateTestProvider();
        
        Assert.Empty(provider.GetRoute("Sol", "Unknown"));
        Assert.Empty(provider.GetRoute("Unknown", "Sol"));
    }
    
    [Fact]
    public void StarSystemInfo_DistanceTo_CalculatesCorrectly()
    {
        var provider = CreateTestProvider();
        
        var sol = provider.GetSystemByName("Sol")!;
        var alphaCentauri = provider.GetSystemByName("Alpha Centauri")!;
        
        var distance = sol.DistanceTo(alphaCentauri);
        var reverseDistance = alphaCentauri.DistanceTo(sol);
        
        // Distance should be the same in both directions
        Assert.Equal(distance, reverseDistance);
        
        // Alpha Centauri is approximately 4.37 light years from Sol
        Assert.True(distance > 4f && distance < 5f);
    }
    
    [Fact]
    public void StarSystemInfo_Position_IsCorrect()
    {
        var provider = CreateTestProvider();
        
        var sol = provider.GetSystemByName("Sol")!;
        
        Assert.Equal(0f, sol.Position.X);
        Assert.Equal(0f, sol.Position.Y);
        Assert.Equal(0f, sol.Position.Z);
    }
    
    [Fact]
    public void StarSystemInfo_RequirePermit_IsCorrect()
    {
        var provider = CreateTestProvider();
        
        var sol = provider.GetSystemByName("Sol")!;
        var alphaCentauri = provider.GetSystemByName("Alpha Centauri")!;
        
        Assert.True(sol.RequirePermit);
        Assert.Equal("Sol", sol.PermitName);
        Assert.False(alphaCentauri.RequirePermit);
        Assert.Null(alphaCentauri.PermitName);
    }
}

/// <summary>
/// Tests for the StarSystemDatabase class
/// </summary>
public class StarSystemDatabaseTests
{
    [Fact]
    public void EmptyDatabase_HasZeroCount()
    {
        var database = new StarSystemDatabase();
        
        Assert.Equal(0, database.Count);
        Assert.Empty(database.Systems);
    }
    
    [Fact]
    public void Add_IncreasesCount()
    {
        var database = new StarSystemDatabase();
        
        database.Add(new StarSystemRecord { Id = 1, Name = "Test" });
        
        Assert.Equal(1, database.Count);
    }
    
    [Fact]
    public void Constructor_WithSystems_PopulatesLookups()
    {
        var systems = new List<StarSystemRecord>
        {
            new StarSystemRecord { Id = 1, Name = "System1" },
            new StarSystemRecord { Id = 2, Name = "System2" }
        };
        
        var database = new StarSystemDatabase(systems);
        
        Assert.Equal(2, database.Count);
        Assert.NotNull(database.GetByName("System1"));
        Assert.NotNull(database.GetById(2));
    }
    
    [Fact]
    public void GetByName_CaseInsensitive()
    {
        var database = new StarSystemDatabase();
        database.Add(new StarSystemRecord { Id = 1, Name = "TestSystem" });
        
        Assert.NotNull(database.GetByName("testsystem"));
        Assert.NotNull(database.GetByName("TESTSYSTEM"));
        Assert.NotNull(database.GetByName("TestSystem"));
    }
}
