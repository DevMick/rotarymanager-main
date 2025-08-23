using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.Application.DTOs.Authentication
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis")]
        public string LastName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Le club est requis")]
        public Guid ClubId { get; set; } // Plus optionnel !

        [Required(ErrorMessage = "Le numéro membre est requis")]
        [MaxLength(50, ErrorMessage = "Le numéro membre ne doit pas dépasser 50 caractères")]
        public string NumeroMembre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date d'adhésion est requise")]
        [DataType(DataType.Date)]
        public DateTime JoinedDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateAnniversaire { get; set; }
    }
}