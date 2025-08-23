using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Application.DTOs.Authentication
{
    public class RegisterAdminRequest
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom de famille est requis")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        public string? PhoneNumber { get; set; }

        // Club ID optionnel si l'admin gère un club spécifique
        public Guid? ClubId { get; set; }


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
