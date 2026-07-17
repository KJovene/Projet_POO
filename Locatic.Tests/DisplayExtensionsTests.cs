using Locatic.Helpers;
using Locatic.Models;

namespace Locatic.Tests;

/// <summary>
/// Couvre la logique d'affichage pure (pas de base de données) : libellés FR,
/// icônes, catégorie dérivée du nombre de places et sélection d'image.
/// </summary>
public class DisplayExtensionsTests
{
    [Theory]
    [InlineData(Fuel.Petrol, "Essence")]
    [InlineData(Fuel.Diesel, "Diesel")]
    [InlineData(Fuel.Electric, "Électrique")]
    [InlineData(Fuel.Hybrid, "Hybride")]
    [InlineData(Fuel.HybridCell, "Hybride rechargeable")]
    [InlineData(Fuel.GPL, "GPL")]
    public void FrenchLabel_ReturnsExpectedLabel(Fuel fuel, string expected)
    {
        Assert.Equal(expected, fuel.FrenchLabel());
    }

    [Theory]
    [InlineData(Fuel.Electric, "⚡")]
    [InlineData(Fuel.Hybrid, "🔋")]
    [InlineData(Fuel.HybridCell, "🔋")]
    [InlineData(Fuel.GPL, "💨")]
    [InlineData(Fuel.Petrol, "⛽")]
    [InlineData(Fuel.Diesel, "⛽")]
    public void Icon_ReturnsExpectedGlyph(Fuel fuel, string expected)
    {
        Assert.Equal(expected, fuel.Icon());
    }

    [Theory]
    [InlineData(2, "Citadine")]
    [InlineData(4, "Citadine")]
    [InlineData(5, "Berline")]
    [InlineData(7, "SUV / Van")]
    [InlineData(9, "SUV / Van")]
    public void Category_DependsOnNumberOfSeats(int seats, string expected)
    {
        var car = new Car { NumberOfSeats = seats };
        Assert.Equal(expected, car.Category());
    }

    [Fact]
    public void ImageUrl_KnownBrand_ReturnsMappedImage()
    {
        var car = new Car
        {
            LicensePlate = "AA-123-BB",
            CarModel = new CarModel
            {
                Name = "Yaris",
                Brand = new CarBrand { Name = "Toyota", Country = "Japon" }
            }
        };

        Assert.Equal("/images/cars/car-01.jpg", car.ImageUrl());
    }

    [Fact]
    public void ImageUrl_KnownBrand_IsCaseInsensitive()
    {
        var car = new Car
        {
            CarModel = new CarModel
            {
                Name = "X",
                Brand = new CarBrand { Name = "tOyOtA", Country = "Japon" }
            }
        };

        Assert.Equal("/images/cars/car-01.jpg", car.ImageUrl());
    }

    [Fact]
    public void ImageUrl_UnknownBrand_ReturnsStablePooledImage()
    {
        Car Make() => new()
        {
            LicensePlate = "ZZ-999-ZZ",
            CarModel = new CarModel
            {
                Name = "Mystery",
                Brand = new CarBrand { Name = "Marque Inconnue", Country = "?" }
            }
        };

        var first = Make().ImageUrl();
        var second = Make().ImageUrl();

        // Déterministe : deux voitures identiques donnent toujours la même image.
        Assert.Equal(first, second);
        Assert.StartsWith("/images/cars/car-", first);
    }

    [Fact]
    public void ImageUrl_NoBrandNoModel_FallsBackToLicensePlate()
    {
        var car = new Car { LicensePlate = "AB-000-CD" };

        var url = car.ImageUrl();

        Assert.StartsWith("/images/cars/car-", url);
    }
}
