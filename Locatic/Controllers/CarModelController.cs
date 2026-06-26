using Locatic.Data;
using Locatic.Models;
using Locatic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Controllers;

public class CarModelController : Controller
{
    private readonly AppDbContext _context;

    public CarModelController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var models = await _context.CarModels
            .Include(model => model.Brand)
            .OrderBy(model => model.Brand.Name)
            .ThenBy(model => model.Name)
            .ToListAsync();

        return View(models);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateBrandsAsync();
        return View(new CarModelFormVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarModelFormVM vm)
    {
        if (ModelState.IsValid)
        {
            var model = new CarModel
            {
                Name = vm.Name,
                BrandId = vm.BrandId
            };

            _context.CarModels.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateBrandsAsync(vm.BrandId);
        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _context.CarModels.FindAsync(id);
        if (model == null) return NotFound();

        await PopulateBrandsAsync(model.BrandId);

        var vm = new CarModelFormVM
        {
            Id = model.Id,
            Name = model.Name,
            BrandId = model.BrandId
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CarModelFormVM vm)
    {
        if (id != vm.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var model = await _context.CarModels.FindAsync(id);
            if (model == null) return NotFound();

            model.Name = vm.Name;
            model.BrandId = vm.BrandId;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateBrandsAsync(vm.BrandId);
        return View(vm);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var model = await _context.CarModels
            .Include(carModel => carModel.Brand)
            .Include(carModel => carModel.Cars)
            .FirstOrDefaultAsync(carModel => carModel.Id == id);

        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var model = await _context.CarModels
            .Include(carModel => carModel.Brand)
            .Include(carModel => carModel.Cars)
            .FirstOrDefaultAsync(carModel => carModel.Id == id);

        if (model == null) return NotFound();

        if (model.Cars.Count > 0)
        {
            ViewData["DeleteError"] = "Impossible de supprimer un modèle qui est encore utilisé par des voitures.";
            return View("Delete", model);
        }

        _context.CarModels.Remove(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateBrandsAsync(int? selectedId = null)
    {
        var brands = await _context.CarBrands
            .OrderBy(brand => brand.Name)
            .ToListAsync();

        ViewBag.BrandId = new SelectList(brands, "Id", "Name", selectedId);
    }
}