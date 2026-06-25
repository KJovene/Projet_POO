using System.ComponentModel.DataAnnotations;

namespace Locatic.Models.ViewModels
{
    public class ClientFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(50, ErrorMessage = "50 caractères maximum")]
        [Display(Name = "Prénom")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(50, ErrorMessage = "50 caractères maximum")]
        [Display(Name = "Nom")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le numéro de téléphone est requis")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [Display(Name = "Téléphone")]
        public string PhoneNumber { get; set; }
    }
}