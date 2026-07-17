using Locatic.Controllers;
using Locatic.Models;
using Locatic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Locatic.Tests;

/// <summary>
/// Vérifie le CRUD Voiture : création, détails, suppression et cas NotFound.
/// </summary>
public class CarControllerTests
{
    [Fact]
    public async Task Create_ValidCar_IsPersistedAndRedirects()
    {
        using var context = TestHelpers.NewContext();
        // On a besoin d'un modèle existant pour rattacher la voiture.
        var seededCar = TestHelpers.SeedCar(context);
        var modelId = seededCar.CarModelId;

        var controller = new CarController(context);
        var vm = new CarCreateVM
        {
            LicensePlate = "CC-333-CC",
            CarModelId = modelId,
            Year = 2023,
            FuelType = Fuel.Electric,
            NumberOfSeats = 5,
            DailyPrice = 55m
        };

        var result = await controller.Create(vm);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(2, context.Cars.Count());
    }

    [Fact]
    public async Task Details_ExistingCar_ReturnsViewWithCar()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var controller = new CarController(context);

        var result = await controller.Details(car.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Car>(view.Model);
        Assert.Equal(car.Id, model.Id);
        Assert.Equal("Toyota", model.CarModel.Brand.Name);
    }

    [Fact]
    public async Task Details_UnknownCar_ReturnsNotFound()
    {
        using var context = TestHelpers.NewContext();
        var controller = new CarController(context);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesCar()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var controller = new CarController(context);

        var result = await controller.DeleteConfirmed(car.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(context.Cars);
    }

    [Fact]
    public async Task Edit_MismatchedId_ReturnsNotFound()
    {
        using var context = TestHelpers.NewContext();
        var controller = new CarController(context);

        var result = await controller.Edit(1, new Car { Id = 2 });

        Assert.IsType<NotFoundResult>(result);
    }
}
