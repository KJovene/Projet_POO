using System.ComponentModel.DataAnnotations;

namespace Locatic.Models.ViewModels
{
    public class CarModelFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom du modèle est requis")]
        [StringLength(50, ErrorMessage = "50 caractères maximum")]
        [Display(Name = "Modèle")]
        public string Name { get; set; }

        [Required(ErrorMessage = "La marque est requise")]
        [Range(1, int.MaxValue, ErrorMessage = "Sélectionne une marque valide")]
        [Display(Name = "Marque")]
        public int BrandId { get; set; }
    }
}