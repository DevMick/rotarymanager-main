using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.API.DTOs.Authentication
{
    public class ClubDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string? Position { get; set; } // Position de l'utilisateur dans ce club
    }

    public class UpdateClubInfoRequest
    {
        [Required(ErrorMessage = "Le nom du club est requis")]
        [StringLength(200, ErrorMessage = "Le nom du club ne peut pas dépasser 200 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code du club est requis")]
        [StringLength(10, ErrorMessage = "Le code du club ne peut pas dépasser 10 caractères")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        public string? Description { get; set; }

        [StringLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "Le pays ne peut pas dépasser 100 caractères")]
        public string? Country { get; set; }

        [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide")]
        [StringLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        public string? Email { get; set; }

        [Url(ErrorMessage = "L'URL du site web n'est pas valide")]
        [StringLength(200, ErrorMessage = "L'URL du site web ne peut pas dépasser 200 caractères")]
        public string? Website { get; set; }

        [Url(ErrorMessage = "L'URL du logo n'est pas valide")]
        [StringLength(200, ErrorMessage = "L'URL du logo ne peut pas dépasser 200 caractères")]
        public string? LogoUrl { get; set; }

        public DateTime? FoundedDate { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CreateClubRequest
    {
        [Required(ErrorMessage = "Le nom du club est requis")]
        [StringLength(200, ErrorMessage = "Le nom du club ne peut pas dépasser 200 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code du club est requis")]
        [StringLength(10, ErrorMessage = "Le code du club ne peut pas dépasser 10 caractères")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        public string? Description { get; set; }

        [StringLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "Le pays ne peut pas dépasser 100 caractères")]
        public string? Country { get; set; }

        [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide")]
        [StringLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        public string? Email { get; set; }

        [Url(ErrorMessage = "L'URL du site web n'est pas valide")]
        [StringLength(200, ErrorMessage = "L'URL du site web ne peut pas dépasser 200 caractères")]
        public string? Website { get; set; }

        [Url(ErrorMessage = "L'URL du logo n'est pas valide")]
        [StringLength(200, ErrorMessage = "L'URL du logo ne peut pas dépasser 200 caractères")]
        public string? LogoUrl { get; set; }

        public DateTime? FoundedDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}