using Locatic.Data;
using Locatic.Models;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Tests;

/// <summary>
/// Utilitaires partagés par les tests : création d'un AppDbContext en mémoire
/// (base isolée par test grâce à un nom de base unique) et fabriques d'entités.
/// </summary>
internal static class TestHelpers
{
    public static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Insère une marque, un modèle et une voiture liés, et renvoie la voiture.</summary>
    public static Car SeedCar(AppDbContext context, string plate = "AA-123-BB")
    {
        var brand = new CarBrand { Name = "Toyota", Country = "Japon" };
        var model = new CarModel { Name = "Yaris", Brand = brand };
        var car = new Car
        {
            LicensePlate = plate,
            Year = 2022,
            DailyPrice = 40m,
            NumberOfSeats = 5,
            FuelType = Fuel.Petrol,
            CarModel = model
        };
        context.Cars.Add(car);
        context.SaveChanges();
        return car;
    }

    public static Client SeedClient(AppDbContext context, string last = "Dupont")
    {
        var client = new Client
        {
            FirstName = "Jean",
            LastName = last,
            Email = "jean@example.com",
            PhoneNumber = "0600000000"
        };
        context.Clients.Add(client);
        context.SaveChanges();
        return client;
    }
}
