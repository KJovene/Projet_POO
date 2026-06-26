using Locatic.Models;

namespace Locatic.Models
{
    public class CarModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BrandId { get; set; }
        public CarBrand Brand { get; set; }
        public List<Car> Cars { get; set; } = new ();
    }
}