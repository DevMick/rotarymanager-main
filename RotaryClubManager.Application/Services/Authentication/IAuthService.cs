using RotaryClubManager.Application.DTOs.Authentication;

namespace RotaryClubManager.Application.Services.Authentication
{
    public interface IAuthService
    {
        Task<List<ClubMemberDto>> GetClubMembersAsync(Guid clubId);
        Task<AuthResponseDto> RegisterAsync(RegisterRequest request);
        Task<AuthResponseDto> LoginAsync(LoginRequest request);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<UserDto> GetUserByIdAsync(string userId, Guid? clubId = null);

        /// <summary>
        /// Enregistre un nouvel administrateur
        /// </summary>
        /// <param name="request">Les données d'enregistrement de l'admin</param>
        /// <returns>Résultat de l'enregistrement</returns>
        Task<AuthResponseDto> RegisterAdminAsync(RegisterAdminRequest request);

        /// <summary>
        /// Vérifie s'il existe déjà un administrateur dans le système
        /// </summary>
        /// <returns>True si un admin existe, false sinon</returns>
        Task<bool> AdminExistsAsync();

        // Méthodes de débogage temporaires
        /// <summary>
        /// Méthode de débogage pour tester la connexion avec informations détaillées
        /// </summary>
        /// <param name="request">Données de connexion</param>
        /// <returns>Résultat avec informations de débogage</returns>
        Task<AuthResponseDto> DebugLoginAsync(LoginRequest request);

        /// <summary>
        /// Crée un utilisateur de test pour le débogage
        /// </summary>
        /// <returns>Résultat de la création</returns>
        Task<AuthResponseDto> CreateTestUserAsync();
    }
}
