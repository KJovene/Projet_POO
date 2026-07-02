using Locatic.Controllers;
using Locatic.Models;
using Locatic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Locatic.Tests;

/// <summary>
/// Vérifie la logique métier des réservations : création valide, refus des
/// chevauchements de dates sur une même voiture, contrôle des dates et
/// comportements NotFound / redirections.
/// </summary>
public class ReservationControllerTests
{
    [Fact]
    public async Task Create_ValidReservation_IsPersistedAndRedirects()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var client = TestHelpers.SeedClient(context);
        var controller = new ReservationController(context);

        var vm = new ReservationFormVM
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 5)
        };

        var result = await controller.Create(vm);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ReservationController.Index), redirect.ActionName);
        Assert.Single(context.Reservations);
    }

    [Fact]
    public async Task Create_EndDateBeforeStartDate_AddsModelErrorAndDoesNotPersist()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var client = TestHelpers.SeedClient(context);
        var controller = new ReservationController(context);

        var vm = new ReservationFormVM
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 10),
            EndDate = new DateTime(2026, 7, 5)
        };

        var result = await controller.Create(vm);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(ReservationFormVM.EndDate)));
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task Create_OverlappingReservationSameCar_IsRejected()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var client = TestHelpers.SeedClient(context);

        context.Reservations.Add(new Reservation
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 10)
        });
        context.SaveChanges();

        var controller = new ReservationController(context);
        var vm = new ReservationFormVM
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 5), // chevauche la période existante
            EndDate = new DateTime(2026, 7, 8)
        };

        var result = await controller.Create(vm);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(ReservationFormVM.CarId)));
        Assert.Single(context.Reservations); // rien de nouveau
    }

    [Fact]
    public async Task Create_AdjacentDatesSameCar_IsAllowed()
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

        var controller = new ReservationController(context);
        var vm = new ReservationFormVM
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 5),
            EndDate = new DateTime(2026, 7, 8)
        };

        var result = await controller.Create(vm);

        // La validation refuse le partage de la borne (5 <= 8 && 5 <= 5 -> chevauchement).
        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(ReservationFormVM.CarId)));
    }

    [Fact]
    public async Task Create_OverlapOnDifferentCar_IsAllowed()
    {
        using var context = TestHelpers.NewContext();
        var car1 = TestHelpers.SeedCar(context, "AA-111-AA");
        var car2 = TestHelpers.SeedCar(context, "BB-222-BB");
        var client = TestHelpers.SeedClient(context);

        context.Reservations.Add(new Reservation
        {
            ClientId = client.Id,
            CarId = car1.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 10)
        });
        context.SaveChanges();

        var controller = new ReservationController(context);
        var vm = new ReservationFormVM
        {
            ClientId = client.Id,
            CarId = car2.Id, // autre voiture, même période
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 10)
        };

        var result = await controller.Create(vm);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(2, context.Reservations.Count());
    }

    [Fact]
    public async Task Edit_ExistingReservation_UpdatesDates()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var client = TestHelpers.SeedClient(context);
        var reservation = new Reservation
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 5)
        };
        context.Reservations.Add(reservation);
        context.SaveChanges();

        var controller = new ReservationController(context);
        var vm = new ReservationFormVM
        {
            Id = reservation.Id,
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 3)
        };

        var result = await controller.Edit(reservation.Id, vm);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = context.Reservations.Single();
        Assert.Equal(new DateTime(2026, 8, 1), updated.StartDate);
        Assert.Equal(new DateTime(2026, 8, 3), updated.EndDate);
    }

    [Fact]
    public async Task Edit_DoesNotConflictWithItself()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var client = TestHelpers.SeedClient(context);
        var reservation = new Reservation
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 10)
        };
        context.Reservations.Add(reservation);
        context.SaveChanges();

        var controller = new ReservationController(context);
        var vm = new ReservationFormVM
        {
            Id = reservation.Id,
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 2), // se recouvre lui-même, doit être ignoré
            EndDate = new DateTime(2026, 7, 9)
        };

        var result = await controller.Edit(reservation.Id, vm);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Edit_MismatchedId_ReturnsNotFound()
    {
        using var context = TestHelpers.NewContext();
        var controller = new ReservationController(context);
        var vm = new ReservationFormVM { Id = 2 };

        var result = await controller.Edit(1, vm);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_UnknownId_ReturnsNotFound()
    {
        using var context = TestHelpers.NewContext();
        var controller = new ReservationController(context);

        var result = await controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesReservation()
    {
        using var context = TestHelpers.NewContext();
        var car = TestHelpers.SeedCar(context);
        var client = TestHelpers.SeedClient(context);
        var reservation = new Reservation
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 5)
        };
        context.Reservations.Add(reservation);
        context.SaveChanges();

        var controller = new ReservationController(context);
        var result = await controller.DeleteConfirmed(reservation.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task DeleteConfirmed_UnknownId_ReturnsNotFound()
    {
        using var context = TestHelpers.NewContext();
        var controller = new ReservationController(context);

        var result = await controller.DeleteConfirmed(123);

        Assert.IsType<NotFoundResult>(result);
    }
}
