using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.API.DTOs.Authentication
{
    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid ClubId { get; set; } // Club auquel l'utilisateur appartient
        public string? Position { get; set; } // Position dans le club (ex: Président, Secrétaire)
        [Required]
        public DateTime JoinedDate { get; set; }
    }
}
