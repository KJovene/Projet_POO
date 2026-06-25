using Locatic.Data;
using Locatic.Models;
using Locatic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Controllers;

public class ReservationController : Controller
{
    private readonly AppDbContext _context;

    public ReservationController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var reservations = await _context.Reservations
            .Include(r => r.Client)
            .Include(r => r.Car)
                .ThenInclude(c => c.CarModel)
                    .ThenInclude(m => m.Brand)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();

        return View(reservations);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateClientsAsync();
        await PopulateCarsAsync();
        return View(new ReservationFormVM
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationFormVM vm)
    {
        await ValidateReservationAsync(vm);

        if (ModelState.IsValid)
        {
            var reservation = new Reservation
            {
                ClientId = vm.ClientId,
                CarId = vm.CarId,
                StartDate = vm.StartDate.Date,
                EndDate = vm.EndDate.Date
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateClientsAsync(vm.ClientId);
        await PopulateCarsAsync(vm.CarId);
        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        await PopulateClientsAsync(reservation.ClientId);
        await PopulateCarsAsync(reservation.CarId);

        var vm = new ReservationFormVM
        {
            Id = reservation.Id,
            ClientId = reservation.ClientId,
            CarId = reservation.CarId,
            StartDate = reservation.StartDate,
            EndDate = reservation.EndDate
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReservationFormVM vm)
    {
        if (id != vm.Id) return NotFound();

        await ValidateReservationAsync(vm);

        if (ModelState.IsValid)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            reservation.ClientId = vm.ClientId;
            reservation.CarId = vm.CarId;
            reservation.StartDate = vm.StartDate.Date;
            reservation.EndDate = vm.EndDate.Date;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateClientsAsync(vm.ClientId);
        await PopulateCarsAsync(vm.CarId);
        return View(vm);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Client)
            .Include(r => r.Car)
                .ThenInclude(c => c.CarModel)
                    .ThenInclude(m => m.Brand)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound();
        return View(reservation);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateClientsAsync(int? selectedId = null)
    {
        var clients = await _context.Clients
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new
            {
                c.Id,
                DisplayName = c.LastName + " " + c.FirstName
            })
            .ToListAsync();

        ViewBag.ClientId = new SelectList(clients, "Id", "DisplayName", selectedId);
    }

    private async Task PopulateCarsAsync(int? selectedId = null)
    {
        var cars = await _context.Cars
            .Include(c => c.CarModel)
                .ThenInclude(m => m.Brand)
            .OrderBy(c => c.CarModel.Brand.Name)
            .ThenBy(c => c.CarModel.Name)
            .Select(c => new
            {
                c.Id,
                DisplayName = c.CarModel.Brand.Name + " " + c.CarModel.Name + " - " + c.LicensePlate
            })
            .ToListAsync();

        ViewBag.CarId = new SelectList(cars, "Id", "DisplayName", selectedId);
    }

    private async Task ValidateReservationAsync(ReservationFormVM vm)
    {
        if (vm.EndDate.Date < vm.StartDate.Date)
        {
            ModelState.AddModelError(nameof(vm.EndDate),
                "La date de fin ne peut pas être antérieure à la date de début.");
        }

        var overlaps = await _context.Reservations.AnyAsync(r =>
            r.Id != vm.Id &&
            r.CarId == vm.CarId &&
            r.StartDate.Date <= vm.EndDate.Date &&
            vm.StartDate.Date <= r.EndDate.Date);

        if (overlaps)
        {
            ModelState.AddModelError(nameof(vm.CarId),
                "Cette voiture est déjà réservée sur la période demandée.");
        }
    }
}