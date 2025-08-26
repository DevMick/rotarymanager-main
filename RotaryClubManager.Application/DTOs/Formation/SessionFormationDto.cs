using RotaryClubManager.Domain.Entities.Formation;

namespace RotaryClubManager.Application.DTOs.Formation
{
    public class SessionFormationDto
    {
        public Guid Id { get; set; }
        public string MembreId { get; set; } = string.Empty;
        public string NomMembre { get; set; } = string.Empty;
        public Guid DocumentFormationId { get; set; }
        public string TitreDocument { get; set; } = string.Empty;
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public int ScoreActuel { get; set; }
        public int ScoreObjectif { get; set; }
        public StatutSession Statut { get; set; }
        public int NombreQuestions { get; set; }
        public int NombreReponsesCorrectes { get; set; }
        public double PourcentageReussite { get; set; }
    }

    public class CreateSessionFormationDto
    {
        public Guid DocumentFormationId { get; set; }
        public int ScoreObjectif { get; set; } = 80;
    }

    public class UpdateSessionFormationDto
    {
        public int ScoreActuel { get; set; }
        public StatutSession Statut { get; set; }
    }
}
