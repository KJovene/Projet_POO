using System.ComponentModel.DataAnnotations;

namespace Locatic.Models.ViewModels
{
    public class ReservationFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le client est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "Sélectionne un client valide")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "La voiture est requise")]
        [Range(1, int.MaxValue, ErrorMessage = "Sélectionne une voiture valide")]
        [Display(Name = "Voiture")]
        public int CarId { get; set; }

        [Required(ErrorMessage = "La date de début est requise")]
        [DataType(DataType.Date)]
        [Display(Name = "Date de début")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "La date de fin est requise")]
        [DataType(DataType.Date)]
        [Display(Name = "Date de fin")]
        public DateTime EndDate { get; set; }
    }
}