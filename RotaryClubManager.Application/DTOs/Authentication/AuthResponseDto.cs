using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Application.DTOs.Authentication
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public string[] Errors { get; set; } = Array.Empty<string>();
        public DateTime DateAnniversaire { get; set; }

        // Ajout des propriétés Club
        public Guid? ClubId { get; set; }
        public string? ClubName { get; set; }
        public string NumeroMembre { get; set; } = string.Empty;

        // Pour le debug
        public Dictionary<string, string>? DebugInfo { get; set; }
    }
    public class ClubMemberDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }

        // Date d'inscription sur la plateforme
        public DateTime UserJoinedDate { get; set; }

        // Date d'adhésion au club (c'est celle qui vous intéresse pour l'affichage)
        public DateTime ClubJoinedDate { get; set; }

        public bool IsActive { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();

        // Informations spécifiques au club
        public Guid ClubId { get; set; }
        public string? ClubName { get; set; }
        public string NumeroMembre { get; set; } = string.Empty;
    }
}
