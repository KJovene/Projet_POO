using Locatic.Data;
using Locatic.Models;
using Locatic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Controllers;

public class CarBrandController : Controller
{
    private readonly AppDbContext _context;

    public CarBrandController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var brands = await _context.CarBrands
            .OrderBy(brand => brand.Name)
            .ToListAsync();

        return View(brands);
    }

    public IActionResult Create()
    {
        return View(new CarBrandFormVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarBrandFormVM vm)
    {
        if (ModelState.IsValid)
        {
            var brand = new CarBrand
            {
                Name = vm.Name,
                Country = vm.Country
            };

            _context.CarBrands.Add(brand);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var brand = await _context.CarBrands.FindAsync(id);
        if (brand == null) return NotFound();

        var vm = new CarBrandFormVM
        {
            Id = brand.Id,
            Name = brand.Name,
            Country = brand.Country
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CarBrandFormVM vm)
    {
        if (id != vm.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var brand = await _context.CarBrands.FindAsync(id);
            if (brand == null) return NotFound();

            brand.Name = vm.Name;
            brand.Country = vm.Country;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var brand = await _context.CarBrands
            .Include(carBrand => carBrand.CarModels)
            .FirstOrDefaultAsync(carBrand => carBrand.Id == id);

        if (brand == null) return NotFound();
        return View(brand);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var brand = await _context.CarBrands
            .Include(carBrand => carBrand.CarModels)
            .FirstOrDefaultAsync(carBrand => carBrand.Id == id);

        if (brand == null) return NotFound();

        if (brand.CarModels.Count > 0)
        {
            ViewData["DeleteError"] = "Impossible de supprimer une marque qui possède encore des modèles.";
            return View("Delete", brand);
        }

        _context.CarBrands.Remove(brand);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}