using Locatic.Models;

namespace Locatic.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public Client Client { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}