namespace RotaryClubManager.API.DTOs.Authentication
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
        public DateTime DateAnniversaire { get; set; }
        public UserDto? User { get; set; }
    }
}
