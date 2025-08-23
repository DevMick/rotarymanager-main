using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Application.DTOs.Authentication
{
    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Department { get; set; } // Ajout de Department
        public string? Position { get; set; } // Ajout de Position
        public DateTime JoinedDate { get; set; }
        public bool IsActive { get; set; }
        public Guid? PrimaryClubId { get; set; }
        public string? PrimaryClubName { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public string NumeroMembre { get; set; } = string.Empty;
        public DateTime DateAnniversaire { get; set; }
    }
}
