using Locatic.Data;
using Locatic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Controllers;

public class CenterController : Controller
{
    private readonly AppDbContext _context;

    public CenterController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new CenterIndexVM
        {
            BrandCount = await _context.CarBrands.CountAsync(),
            ModelCount = await _context.CarModels.CountAsync(),
            CarCount = await _context.Cars.CountAsync()
        };

        return View(vm);
    }
}
