using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Locatic.Data;
using Locatic.Models;
using Locatic.Models.ViewModels;

namespace Locatic.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        // SQLite can't ORDER BY a decimal, so sort the featured cars in memory.
        var cars = await _context.Cars
            .Include(c => c.CarModel)
                .ThenInclude(m => m.Brand)
            .ToListAsync();

        var vm = new HomeIndexVM
        {
            FeaturedCars = cars
                .OrderBy(c => c.DailyPrice)
                .Take(6)
                .ToList(),
            CarCount = cars.Count,
            ClientCount = await _context.Clients.CountAsync(),
            ReservationCount = await _context.Reservations.CountAsync(),
            ActiveReservationCount = await _context.Reservations
                .CountAsync(r => r.StartDate <= today && r.EndDate >= today)
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
