using System.ComponentModel.DataAnnotations;

namespace Locatic.Models.ViewModels
{
    public class CarBrandFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de la marque est requis")]
        [StringLength(50, ErrorMessage = "50 caractères maximum")]
        [Display(Name = "Marque")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Le pays est requis")]
        [StringLength(50, ErrorMessage = "50 caractères maximum")]
        [Display(Name = "Pays")]
        public string Country { get; set; }
    }
}