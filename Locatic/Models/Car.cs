using Locatic.Models;

namespace Locatic.Models

{
    public class Car
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; }
        public int Year { get; set; }
        public decimal DailyPrice { get; set; }
        public int NumberOfSeats { get; set; }
        public Fuel FuelType { get; set; }
        public int CarModelId { get; set; }
        public CarModel CarModel { get; set; }
        public List<Reservation> Reservations { get; set; } = new ();
    }
}