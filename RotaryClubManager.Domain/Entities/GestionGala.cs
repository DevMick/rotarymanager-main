using RotaryClubManager.Domain.Identity;

namespace RotaryClubManager.Domain.Entities
{
    // Modèle principal GALA
    public class Gala
    {
        public Guid Id { get; set; }

        public string Libelle { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string Lieu { get; set; } = string.Empty;

        public int NombreTables { get; set; }

        public int NombreSouchesTickets { get; set; }

        public int QuantiteParSoucheTickets { get; set; }

        public int NombreSouchesTombola { get; set; }

        public int QuantiteParSoucheTombola { get; set; }

        // Navigation properties
        public virtual ICollection<GalaInvites> Invites { get; set; } = new List<GalaInvites>();
        public virtual ICollection<GalaTable> Tables { get; set; } = new List<GalaTable>();
        public virtual ICollection<GalaTicket> Tickets { get; set; } = new List<GalaTicket>();
        public virtual ICollection<GalaTombola> Tombolas { get; set; } = new List<GalaTombola>();
    }

    // Modèle GalaInvites
    public class GalaInvites
    {
        public Guid Id { get; set; }

        public string Nom_Prenom { get; set; } = string.Empty;

        public bool? Present { get; set; } = false;

        // Relation avec le gala
        public Guid GalaId { get; set; }
        public virtual Gala Gala { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<GalaTableAffectation> TableAffectations { get; set; } = new List<GalaTableAffectation>();
    }

    // Modèle GalaTable
    public class GalaTable
    {
        public Guid Id { get; set; }

        public string TableLibelle { get; set; } = string.Empty;

        // Relation avec le gala
        public Guid GalaId { get; set; }
        public virtual Gala Gala { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<GalaTableAffectation> TableAffectations { get; set; } = new List<GalaTableAffectation>();
    }

    // Modèle GalaTableAffectation (Table de liaison)
    public class GalaTableAffectation
    {
        public Guid Id { get; set; }

        // Relation avec la table
        public Guid GalaTableId { get; set; }
        public virtual GalaTable GalaTable { get; set; } = null!;
        public DateTime DateAjout { get; set; } = DateTime.UtcNow;

        // Relation avec l'invité
        public Guid GalaInvitesId { get; set; }
        public virtual GalaInvites GalaInvites { get; set; } = null!;
    }

    // Modèle GalaTicket modifié
    public class GalaTicket
    {
        public Guid Id { get; set; }
        public int Quantite { get; set; }

        // Nouveau champ pour les participants externes
        public string? Externe { get; set; }

        // Relation avec le gala
        public Guid GalaId { get; set; }
        public virtual Gala Gala { get; set; } = null!;

        // Relation avec le membre (maintenant optionnelle)
        public string? MembreId { get; set; }
        public virtual ApplicationUser? Membre { get; set; }
    }

    // Modèle GalaTombola mis à jour
    public class GalaTombola
    {
        public Guid Id { get; set; }
        public int Quantite { get; set; }

        // Nouveau champ pour les participants externes
        public string? Externe { get; set; }

        // Relation avec le gala
        public Guid GalaId { get; set; }
        public virtual Gala Gala { get; set; } = null!;

        // Relation avec le membre (maintenant optionnelle)
        public string? MembreId { get; set; }
        public virtual ApplicationUser? Membre { get; set; }
    }
}