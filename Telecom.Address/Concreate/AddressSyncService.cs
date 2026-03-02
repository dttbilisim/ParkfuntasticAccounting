using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telecom.Address.Abstract;
namespace Telecom.Address.Concreate;
public class AddressSyncService : IAddressSyncService
{
    private readonly IAddressApiService _api;
    private readonly IUnitOfWork<ApplicationDbContext> _db;
    private readonly IServiceProvider _serviceProvider;

    public AddressSyncService(IAddressApiService api, IUnitOfWork<ApplicationDbContext> db, IServiceProvider serviceProvider)
    {
        _api = api;
        _db = db;
        _serviceProvider = serviceProvider;
    }

    public async Task SyncAllAsync()
    {
        try
        {
            Console.WriteLine("Starting address sync...");
            var cities = await _api.GetCitiesAsync();
            Console.WriteLine($"Found {cities.Count} cities");
            
            var allTowns = new List<Town>();
            var allNeighboors = new List<Neighboor>();
            var allStreets = new List<Street>();
            var allBuildings = new List<Building>();
            var allHomes = new List<Home>();

            // Collect all data first
            int cityCount = 0;
            foreach(var city in cities){
                cityCount++;
                Console.WriteLine($"Processing city {cityCount}/{cities.Count}: {city.Name}");
                if (string.IsNullOrEmpty(city.Code) || string.IsNullOrEmpty(city.Name)) continue;
                
                var cityId = int.Parse(city.Code);
                var towns = await _api.GetTownsAsync(cityId);
                
                foreach(var town in towns){
                    if (string.IsNullOrEmpty(town.Code) || string.IsNullOrEmpty(town.Name)) continue;
                    
                    var townId = int.Parse(town.Code);
                    allTowns.Add(new Town{Id = townId, Name = town.Name, CityId = cityId});
                    
                    var neighs = await _api.GetNeighboorsAsync(townId);
                    foreach(var n in neighs){
                        if (string.IsNullOrEmpty(n.Code) || string.IsNullOrEmpty(n.Name)) continue;
                        
                        var neighboorId = int.Parse(n.Code);
                        allNeighboors.Add(new Neighboor{Id = neighboorId, Name = n.Name, TownId = townId});
                        
                        var streets = await _api.GetStreetsAsync(neighboorId);
                        foreach(var s in streets){
                            if (string.IsNullOrEmpty(s.Code) || string.IsNullOrEmpty(s.Name)) continue;
                            
                            var streetId = int.Parse(s.Code);
                            allStreets.Add(new Street{Id = streetId, Name = s.Name, NeighboorId = neighboorId});
                            
                            var buildings = await _api.GetBuildingsAsync(streetId);
                            foreach(var b in buildings){
                                if (string.IsNullOrEmpty(b.Code) || string.IsNullOrEmpty(b.Name)) continue;
                                
                                var buildingId = int.Parse(b.Code);
                                allBuildings.Add(new Building{Id = buildingId, Name = b.Name, StreetId = streetId});
                                
                                var homes = await _api.GetHomesAsync(buildingId);
                                foreach(var h in homes){
                                    if (string.IsNullOrEmpty(h.Code) || string.IsNullOrEmpty(h.Name) || h.Name.Contains("null")) continue;
                                    
                                    var homeId = int.Parse(h.Code);
                                    // Clean up the name - remove "Ic Kapi(Daire) No :" prefix if present
                                    var cleanName = h.Name.Replace("Ic Kapi(Daire) No :", "").Trim();
                                    if (string.IsNullOrEmpty(cleanName)) continue;
                                    
                                    allHomes.Add(new Home{Id = homeId, Name = cleanName, BuildingId = buildingId});
                                }
                            }
                        }
                    }
                }
            }

            // Bulk operations
            Console.WriteLine($"Starting bulk operations...");
            Console.WriteLine($"Cities: {cities.Count}, Towns: {allTowns.Count}, Neighboors: {allNeighboors.Count}, Streets: {allStreets.Count}, Buildings: {allBuildings.Count}, Homes: {allHomes.Count}");
            
            await BulkUpsertCities(cities);
            Console.WriteLine("Cities synced");
            
            await BulkUpsertTowns(allTowns);
            Console.WriteLine("Towns synced");
            
            await BulkUpsertNeighboors(allNeighboors);
            Console.WriteLine("Neighboors synced");
            
            await BulkUpsertStreets(allStreets);
            Console.WriteLine("Streets synced");
            
            await BulkUpsertBuildings(allBuildings);
            Console.WriteLine("Buildings synced");
            
            await BulkUpsertHomes(allHomes);
            Console.WriteLine("Homes synced");
            
            Console.WriteLine("Address sync completed successfully!");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Address sync failed: {e}");
            throw;
        }
    }

    private async Task SyncCityAsync(Dtos.CityDto city)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        var cityId = int.Parse(city.Code);
        var existingCity = await db.DbContext.City.FirstOrDefaultAsync(x => x.Id == cityId);
        if(existingCity == null){
            db.DbContext.City.Add(new City{Id = cityId, Name = city.Name});
        } else {
            existingCity.Name = city.Name;
        }
        await db.SaveChangesAsync();

        var towns = await _api.GetTownsAsync(cityId);
        foreach(var town in towns){
            await SyncTownAsync(town, cityId);
        }
    }

    private async Task SyncTownAsync(Dtos.TownDto town, int cityId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        var townId = int.Parse(town.Code);
        var existingTown = await db.DbContext.Town.FirstOrDefaultAsync(x => x.Id == townId);
        if(existingTown == null){
            db.DbContext.Town.Add(new Town{Id = townId, Name = town.Name, CityId = cityId});
        } else {
            existingTown.Name = town.Name;
            existingTown.CityId = cityId;
        }
        await db.SaveChangesAsync();

        var neighs = await _api.GetNeighboorsAsync(townId);
        foreach(var n in neighs){
            await SyncNeighboorAsync(n, townId);
        }
    }

    private async Task SyncNeighboorAsync(Dtos.NeighboorDto neighboor, int townId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        var neighboorId = int.Parse(neighboor.Code);
        var existingNeighboor = await db.DbContext.Neighboors.FirstOrDefaultAsync(x => x.Id == neighboorId);
        if(existingNeighboor == null){
            db.DbContext.Neighboors.Add(new Neighboor{Id = neighboorId, Name = neighboor.Name, TownId = townId});
        } else {
            existingNeighboor.Name = neighboor.Name;
            existingNeighboor.TownId = townId;
        }
        await db.SaveChangesAsync();

        var streets = await _api.GetStreetsAsync(neighboorId);
        foreach(var s in streets){
            await SyncStreetAsync(s, neighboorId);
        }
    }

    private async Task SyncStreetAsync(Dtos.StreetDto street, int neighboorId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        var streetId = int.Parse(street.Code);
        var existingStreet = await db.DbContext.Street.FirstOrDefaultAsync(x => x.Id == streetId);
        if(existingStreet == null){
            db.DbContext.Street.Add(new Street{Id = streetId, Name = street.Name, NeighboorId = neighboorId});
        } else {
            existingStreet.Name = street.Name;
            existingStreet.NeighboorId = neighboorId;
        }
        await db.SaveChangesAsync();

        var buildings = await _api.GetBuildingsAsync(streetId);
        foreach(var b in buildings){
            await SyncBuildingAsync(b, streetId);
        }
    }

    private async Task SyncBuildingAsync(Dtos.BuildingDto building, int streetId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        var buildingId = int.Parse(building.Code);
        var existingBuilding = await db.DbContext.Buildings.FirstOrDefaultAsync(x => x.Id == buildingId);
        if(existingBuilding == null){
            try
            {
                db.DbContext.Buildings.Add(new Building{Id = buildingId, Name = building.Name, StreetId = streetId});
                await db.SaveChangesAsync();
            }
            catch (Exception ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
            {
                // Building already exists, update it instead
                var existingBuildingRetry = await db.DbContext.Buildings.FirstOrDefaultAsync(x => x.Id == buildingId);
                if(existingBuildingRetry != null)
                {
                    existingBuildingRetry.Name = building.Name;
                    existingBuildingRetry.StreetId = streetId;
                    await db.SaveChangesAsync();
                }
            }
        } else {
            existingBuilding.Name = building.Name;
            existingBuilding.StreetId = streetId;
            await db.SaveChangesAsync();
        }

        var homes = await _api.GetHomesAsync(buildingId);
        foreach(var h in homes){
            await SyncHomeAsync(h, buildingId);
        }
    }

    private async Task SyncHomeAsync(Dtos.HomeDto home, int buildingId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        var homeId = int.Parse(home.Code);
        var existingHome = await db.DbContext.Homes.FirstOrDefaultAsync(x => x.Id == homeId);
        if(existingHome == null){
            try
            {
                db.DbContext.Homes.Add(new Home{Id = homeId, Name = home.Name, BuildingId = buildingId});
                await db.SaveChangesAsync();
            }
            catch (Exception ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
            {
                // Home already exists, update it instead
                var existingHomeRetry = await db.DbContext.Homes.FirstOrDefaultAsync(x => x.Id == homeId);
                if(existingHomeRetry != null)
                {
                    existingHomeRetry.Name = home.Name;
                    existingHomeRetry.BuildingId = buildingId;
                    await db.SaveChangesAsync();
                }
            }
        } else {
            existingHome.Name = home.Name;
            existingHome.BuildingId = buildingId;
            await db.SaveChangesAsync();
        }

        // Temporarily disabled due to API JSON format issue
        // var infos = await _api.GetAddressInfoAsync(homeId);
        // foreach(var info in infos){
        //     await SyncAddressInfoAsync(info, homeId);
        // }
    }

    private async Task SyncAddressInfoAsync(Dtos.AddressInfDto info, int homeId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        var existingInfo = await db.DbContext.AddressInfos.FirstOrDefaultAsync(x => x.Id == info.Id);
        if(existingInfo == null){
            db.DbContext.AddressInfos.Add(new AddressInf{Id = info.Id, Detail = info.Detail, HomeId = homeId});
        } else {
            existingInfo.Detail = info.Detail;
            existingInfo.HomeId = homeId;
        }
        await db.SaveChangesAsync();
    }

    private async Task BulkUpsertCities(List<Dtos.CityDto> cities)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        var cityEntities = cities.Select(c => new City { Id = int.Parse(c.Code), Name = c.Name }).ToList();
        
        // Get existing cities
        var existingIds = await db.DbContext.City.Select(x => x.Id).ToListAsync();
        var newCities = cityEntities.Where(c => !existingIds.Contains(c.Id)).ToList();
        var updateCities = cityEntities.Where(c => existingIds.Contains(c.Id)).ToList();
        
        // Bulk insert new cities
        if (newCities.Any())
        {
            await db.DbContext.City.AddRangeAsync(newCities);
        }
        
        // Update existing cities
        foreach (var city in updateCities)
        {
            var existing = await db.DbContext.City.FirstOrDefaultAsync(x => x.Id == city.Id);
            if (existing != null)
            {
                existing.Name = city.Name;
            }
        }
        
        await db.SaveChangesAsync();
    }

    private async Task BulkUpsertTowns(List<Town> towns)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        // Get existing towns
        var existingIds = await db.DbContext.Town.Select(x => x.Id).ToListAsync();
        var newTowns = towns.Where(t => !existingIds.Contains(t.Id)).ToList();
        var updateTowns = towns.Where(t => existingIds.Contains(t.Id)).ToList();
        
        // Bulk insert new towns
        if (newTowns.Any())
        {
            await db.DbContext.Town.AddRangeAsync(newTowns);
        }
        
        // Update existing towns
        foreach (var town in updateTowns)
        {
            var existing = await db.DbContext.Town.FirstOrDefaultAsync(x => x.Id == town.Id);
            if (existing != null)
            {
                existing.Name = town.Name;
                existing.CityId = town.CityId;
            }
        }
        
        await db.SaveChangesAsync();
    }

    private async Task BulkUpsertNeighboors(List<Neighboor> neighboors)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        // Get existing neighboors
        var existingIds = await db.DbContext.Neighboors.Select(x => x.Id).ToListAsync();
        var newNeighboors = neighboors.Where(n => !existingIds.Contains(n.Id)).ToList();
        var updateNeighboors = neighboors.Where(n => existingIds.Contains(n.Id)).ToList();
        
        // Bulk insert new neighboors
        if (newNeighboors.Any())
        {
            await db.DbContext.Neighboors.AddRangeAsync(newNeighboors);
        }
        
        // Update existing neighboors
        foreach (var neighboor in updateNeighboors)
        {
            var existing = await db.DbContext.Neighboors.FirstOrDefaultAsync(x => x.Id == neighboor.Id);
            if (existing != null)
            {
                existing.Name = neighboor.Name;
                existing.TownId = neighboor.TownId;
            }
        }
        
        await db.SaveChangesAsync();
    }

    private async Task BulkUpsertStreets(List<Street> streets)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        // Get existing streets
        var existingIds = await db.DbContext.Street.Select(x => x.Id).ToListAsync();
        var newStreets = streets.Where(s => !existingIds.Contains(s.Id)).ToList();
        var updateStreets = streets.Where(s => existingIds.Contains(s.Id)).ToList();
        
        // Bulk insert new streets
        if (newStreets.Any())
        {
            await db.DbContext.Street.AddRangeAsync(newStreets);
        }
        
        // Update existing streets
        foreach (var street in updateStreets)
        {
            var existing = await db.DbContext.Street.FirstOrDefaultAsync(x => x.Id == street.Id);
            if (existing != null)
            {
                existing.Name = street.Name;
                existing.NeighboorId = street.NeighboorId;
            }
        }
        
        await db.SaveChangesAsync();
    }

    private async Task BulkUpsertBuildings(List<Building> buildings)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        // Filter out null names and duplicate IDs
        var validBuildings = buildings
            .Where(b => !string.IsNullOrEmpty(b.Name) && b.Id > 0)
            .GroupBy(b => b.Id)
            .Select(g => g.First())
            .ToList();
        
        // Get existing buildings
        var existingIds = await db.DbContext.Buildings.Select(x => x.Id).ToListAsync();
        var newBuildings = validBuildings.Where(b => !existingIds.Contains(b.Id)).ToList();
        var updateBuildings = validBuildings.Where(b => existingIds.Contains(b.Id)).ToList();
        
        // Bulk insert new buildings with try-catch
        if (newBuildings.Any())
        {
            try
            {
                await db.DbContext.Buildings.AddRangeAsync(newBuildings);
                await db.SaveChangesAsync();
            }
            catch (Exception ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
            {
                // If bulk insert fails due to duplicates, try individual inserts
                foreach (var building in newBuildings)
                {
                    try
                    {
                        var exists = await db.DbContext.Buildings.AnyAsync(x => x.Id == building.Id);
                        if (!exists)
                        {
                            db.DbContext.Buildings.Add(building);
                            await db.SaveChangesAsync();
                        }
                    }
                    catch
                    {
                        // Skip this building if it still fails
                        continue;
                    }
                }
            }
        }
        
        // Update existing buildings
        foreach (var building in updateBuildings)
        {
            var existing = await db.DbContext.Buildings.FirstOrDefaultAsync(x => x.Id == building.Id);
            if (existing != null)
            {
                existing.Name = building.Name;
                existing.StreetId = building.StreetId;
            }
        }
        
        await db.SaveChangesAsync();
    }

    private async Task BulkUpsertHomes(List<Home> homes)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        
        // Get existing homes
        var existingIds = await db.DbContext.Homes.Select(x => x.Id).ToListAsync();
        var newHomes = homes.Where(h => !existingIds.Contains(h.Id)).ToList();
        var updateHomes = homes.Where(h => existingIds.Contains(h.Id)).ToList();
        
        // Bulk insert new homes
        if (newHomes.Any())
        {
            await db.DbContext.Homes.AddRangeAsync(newHomes);
        }
        
        // Update existing homes
        foreach (var home in updateHomes)
        {
            var existing = await db.DbContext.Homes.FirstOrDefaultAsync(x => x.Id == home.Id);
            if (existing != null)
            {
                existing.Name = home.Name;
                existing.BuildingId = home.BuildingId;
            }
        }
        
        await db.SaveChangesAsync();
    }
}
