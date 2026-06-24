using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Locatic.Data;
using Locatic.Models;
using Locatic.Models.ViewModels;

namespace Locatic.Controllers;

public class CarController : Controller
{
    private readonly AppDbContext _context;

    public CarController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cars = await _context.Cars
            .Include(c => c.CarModel)
                .ThenInclude(m => m.Brand)
            .ToListAsync();
        return View(cars);
    }

    public async Task<IActionResult> Details(int id)
    {
        var car = await _context.Cars
            .Include(c => c.CarModel)
                .ThenInclude(m => m.Brand)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (car == null) return NotFound();
        return View(car);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCarModelsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarCreateVM vm)
    {
        if (ModelState.IsValid)
        {
            var car = new Car
            {
                LicensePlate = vm.LicensePlate,
                CarModelId = vm.CarModelId,
                Year = vm.Year,
                FuelType = vm.FuelType,
                NumberOfSeats = vm.NumberOfSeats,
                DailyPrice = vm.DailyPrice
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateCarModelsAsync(vm.CarModelId);
        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();

        await PopulateCarModelsAsync(car.CarModelId);
        return View(car);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Car car)
    {
        if (id != car.Id) return NotFound();

        ModelState.Remove(nameof(Car.CarModel));
        if (ModelState.IsValid)
        {
            _context.Update(car);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PopulateCarModelsAsync(car.CarModelId);
        return View(car);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var car = await _context.Cars
            .Include(c => c.CarModel)
                .ThenInclude(m => m.Brand)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (car == null) return NotFound();
        return View(car);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car != null)
        {
            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCarModelsAsync(int? selectedId = null)
    {
        var models = await _context.CarModels
            .Include(m => m.Brand)
            .OrderBy(m => m.Brand.Name)
            .ThenBy(m => m.Name)
            .Select(m => new { m.Id, DisplayName = m.Brand.Name + " " + m.Name })
            .ToListAsync();

        ViewBag.CarModelId = new SelectList(models, "Id", "DisplayName", selectedId);
    }
}
