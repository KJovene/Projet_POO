namespace Locatic.Models.ViewModels
{
    public class HomeIndexVM
    {
        public List<Car> FeaturedCars { get; set; } = new();
        public int CarCount { get; set; }
        public int ClientCount { get; set; }
        public int ReservationCount { get; set; }
        public int ActiveReservationCount { get; set; }
    }
}
