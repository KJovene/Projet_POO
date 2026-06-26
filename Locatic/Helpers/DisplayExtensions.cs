using Locatic.Models;

namespace Locatic.Helpers;

public static class DisplayExtensions
{
    /// <summary>French label for a fuel type.</summary>
    public static string FrenchLabel(this Fuel fuel) => fuel switch
    {
        Fuel.Petrol => "Essence",
        Fuel.Diesel => "Diesel",
        Fuel.Electric => "Électrique",
        Fuel.Hybrid => "Hybride",
        Fuel.HybridCell => "Hybride rechargeable",
        Fuel.GPL => "GPL",
        _ => fuel.ToString()
    };

    /// <summary>Small glyph used in the car cards / specs rows.</summary>
    public static string Icon(this Fuel fuel) => fuel switch
    {
        Fuel.Electric => "⚡",
        Fuel.Hybrid or Fuel.HybridCell => "🔋",
        Fuel.GPL => "💨",
        _ => "⛽"
    };

    /// <summary>Rough category derived from the number of seats (no category field on the model).</summary>
    public static string Category(this Car car) => car.NumberOfSeats switch
    {
        <= 4 => "Citadine",
        5 => "Berline",
        _ => "SUV / Van"
    };

    // Royalty-free car photos (Unsplash licence) bundled under wwwroot/images/cars.
    private static readonly string[] CarImagePool =
    {
        "/images/cars/car-01.jpg", "/images/cars/car-02.jpg", "/images/cars/car-03.jpg",
        "/images/cars/car-04.jpg", "/images/cars/car-05.jpg", "/images/cars/car-06.jpg",
        "/images/cars/car-07.jpg", "/images/cars/car-08.jpg", "/images/cars/car-09.jpg",
        "/images/cars/car-10.jpg"
    };

    private static readonly Dictionary<string, string> BrandImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Toyota"] = "/images/cars/car-01.jpg",
        ["Volkswagen"] = "/images/cars/car-02.jpg",
        ["BMW"] = "/images/cars/car-03.jpg",
        ["Mercedes-Benz"] = "/images/cars/car-04.jpg",
        ["Peugeot"] = "/images/cars/car-05.jpg",
        ["Renault"] = "/images/cars/car-06.jpg",
        ["Ford"] = "/images/cars/car-07.jpg",
        ["Honda"] = "/images/cars/car-08.jpg",
        ["Audi"] = "/images/cars/car-09.jpg",
        ["Citroën"] = "/images/cars/car-10.jpg"
    };

    /// <summary>
    /// Returns a bundled royalty-free photo for the car, chosen by brand.
    /// Unknown brands get a stable fallback so the same car always shows the same image.
    /// </summary>
    public static string ImageUrl(this Car car)
    {
        var brand = car.CarModel?.Brand?.Name;
        if (brand != null && BrandImages.TryGetValue(brand, out var url))
            return url;

        var key = brand ?? car.CarModel?.Name ?? car.LicensePlate ?? string.Empty;
        var hash = 0;
        foreach (var ch in key) hash = (hash * 31 + ch) & 0x7fffffff;
        return CarImagePool[hash % CarImagePool.Length];
    }
}
