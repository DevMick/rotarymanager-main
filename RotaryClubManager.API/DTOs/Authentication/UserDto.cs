namespace RotaryClubManager.API.DTOs.Authentication
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
        public DateTime DateAnniversaire { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public ClubDto? PrimaryClub { get; set; }
        public List<ClubDto> Clubs { get; set; } = new List<ClubDto>();
    }
}
