namespace RotaryClubManager.API.DTOs.Authentication
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid? ClubId { get; set; } // Optionnel, utilisé si l'utilisateur appartient à plusieurs clubs
    }
}
