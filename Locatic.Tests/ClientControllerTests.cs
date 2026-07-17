using Locatic.Controllers;
using Locatic.Models;
using Locatic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Locatic.Tests;

/// <summary>
/// Vérifie le CRUD Client, avec en particulier la garde métier interdisant la
/// suppression d'un client qui possède encore des réservations.
/// </summary>
public class ClientControllerTests
{
    [Fact]
    public async Task Create_ValidClient_IsPersistedAndRedirects()
    {
        using var context = TestHelpers.NewContext();
        var controller = new ClientController(context);
        var vm = new ClientFormVM
        {
            FirstName = "Alice",
            LastName = "Martin",
            Email = "alice@example.com",
            PhoneNumber = "0611111111"
        };

        var result = await controller.Create(vm);

        Assert.IsType<RedirectToActionResult>(result);
        var client = Assert.Single(context.Clients);
        Assert.Equal("Alice", client.FirstName);
    }

    [Fact]
    public async Task Edit_MismatchedId_ReturnsNotFound()
    {
        using var context = TestHelpers.NewContext();
        var controller = new ClientController(context);

        var result = await controller.Edit(1, new ClientFormVM { Id = 2 });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_ValidClient_UpdatesFields()
    {
        using var context = TestHelpers.NewContext();
        var client = TestHelpers.SeedClient(context);
        var controller = new ClientController(context);

        var vm = new ClientFormVM
        {
            Id = client.Id,
            FirstName = "Bob",
            LastName = "Durand",
            Email = "bob@example.com",
            PhoneNumber = "0622222222"
        };

        var result = await controller.Edit(client.Id, vm);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = context.Clients.Single();
        Assert.Equal("Bob", updated.FirstName);
        Assert.Equal("Durand", updated.LastName);
    }

    [Fact]
    public async Task DeleteConfirmed_ClientWithoutReservations_IsRemoved()
    {
        using var context = TestHelpers.NewContext();
        var client = TestHelpers.SeedClient(context);
        var controller = new ClientController(context);

        var result = await controller.DeleteConfirmed(client.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(context.Clients);
    }

    [Fact]
    public async Task DeleteConfirmed_ClientWithReservations_IsBlocked()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var client = TestHelpers.SeedClient(context);
        context.Reservations.Add(new Reservation
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 5)
        });
        context.SaveChanges();

        var controller = new ClientController(context);
        var result = await controller.DeleteConfirmed(client.Id);

        // La suppression est refusée : on renvoie la vue Delete avec un message d'erreur.
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Delete", view.ViewName);
        Assert.NotNull(controller.ViewData["DeleteError"]);
        Assert.Single(context.Clients); // toujours présent
    }

    [Fact]
    public async Task DeleteConfirmed_UnknownId_ReturnsNotFound()
    {
        using var context = TestHelpers.NewContext();
        var controller = new ClientController(context);

        var result = await controller.DeleteConfirmed(404);

        Assert.IsType<NotFoundResult>(result);
    }
}
