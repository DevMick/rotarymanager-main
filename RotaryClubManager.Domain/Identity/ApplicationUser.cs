// Dans RotaryClubManager.Domain/Identity/ApplicationUser.cs
using Microsoft.AspNetCore.Identity; // Import correct
using RotaryClubManager.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.Domain.Identity
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ProfilePictureUrl { get; set; }


        public DateTime JoinedDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        [MaxLength(50)]
        public string NumeroMembre { get; set; } = string.Empty;

        [Required]
        public DateTime DateAnniversaire { get; set; }

        // Navigation properties

        public virtual ICollection<UserClub> UserClubs { get; set; } = new List<UserClub>();
    }
}