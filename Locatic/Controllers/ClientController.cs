using Locatic.Data;
using Locatic.Models;
using Locatic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Controllers;

public class ClientController : Controller
{
    private readonly AppDbContext _context;

    public ClientController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var clients = await _context.Clients
            .OrderBy(client => client.LastName)
            .ThenBy(client => client.FirstName)
            .ToListAsync();

        return View(clients);
    }

    public IActionResult Create()
    {
        return View(new ClientFormVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientFormVM vm)
    {
        if (ModelState.IsValid)
        {
            var client = new Client
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null) return NotFound();

        var vm = new ClientFormVM
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            PhoneNumber = client.PhoneNumber
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClientFormVM vm)
    {
        if (id != vm.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();

            client.FirstName = vm.FirstName;
            client.LastName = vm.LastName;
            client.Email = vm.Email;
            client.PhoneNumber = vm.PhoneNumber;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var client = await _context.Clients
            .Include(currentClient => currentClient.Reservations)
            .FirstOrDefaultAsync(currentClient => currentClient.Id == id);

        if (client == null) return NotFound();
        return View(client);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = await _context.Clients
            .Include(currentClient => currentClient.Reservations)
            .FirstOrDefaultAsync(currentClient => currentClient.Id == id);

        if (client == null) return NotFound();

        if (client.Reservations.Count > 0)
        {
            ViewData["DeleteError"] = "Impossible de supprimer un client qui possède encore des réservations.";
            return View("Delete", client);
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}