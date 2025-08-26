using RotaryClubManager.Domain.Entities.Formation;

namespace RotaryClubManager.Application.DTOs.Formation
{
    public class BadgeFormationDto
    {
        public Guid Id { get; set; }
        public string MembreId { get; set; } = string.Empty;
        public string NomMembre { get; set; } = string.Empty;
        public TypeBadge Type { get; set; }
        public string? DocumentFormationId { get; set; }
        public string? TitreDocument { get; set; }
        public DateTime DateObtention { get; set; }
        public int PointsGagnes { get; set; }
    }

    public class CreateBadgeFormationDto
    {
        public string MembreId { get; set; } = string.Empty;
        public TypeBadge Type { get; set; }
        public string? DocumentFormationId { get; set; }
        public int PointsGagnes { get; set; } = 0;
    }

    public class ProgressionFormationDto
    {
        public string MembreId { get; set; } = string.Empty;
        public string NomMembre { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int NombreBadges { get; set; }
        public int FormationsCompletees { get; set; }
        public int FormationsEnCours { get; set; }
        public double ScoreMoyen { get; set; }
        public List<BadgeFormationDto> Badges { get; set; } = new List<BadgeFormationDto>();
    }
}
