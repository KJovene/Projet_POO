using System.ComponentModel.DataAnnotations;

namespace Locatic.Models.ViewModels
{
    public class CarCreateVM
    {
        [Required(ErrorMessage = "La plaque d'immatriculation est requise")]
        [StringLength(20, ErrorMessage = "20 caractères maximum")]
        [Display(Name = "Plaque d'immatriculation")]
        public string LicensePlate { get; set; }

        [Required(ErrorMessage = "Le modèle est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "Sélectionne un modèle valide")]
        [Display(Name = "Modèle")]
        public int CarModelId { get; set; }

        [Required(ErrorMessage = "L'année est requise")]
        [Range(1900, 2100, ErrorMessage = "Année invalide")]
        [Display(Name = "Année")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Le carburant est requis")]
        [Display(Name = "Carburant")]
        public Fuel FuelType { get; set; }

        [Required(ErrorMessage = "Le nombre de places est requis")]
        [Range(1, 15, ErrorMessage = "Le nombre de places doit être entre 1 et 15")]
        [Display(Name = "Nombre de places")]
        public int NumberOfSeats { get; set; }

        [Required(ErrorMessage = "Le prix journalier est requis")]
        [Range(1, 1000000000, ErrorMessage = "Le prix doit être supérieur à 0")]
        [Display(Name = "Prix journalier")]
        public decimal DailyPrice { get; set; }
    }
}