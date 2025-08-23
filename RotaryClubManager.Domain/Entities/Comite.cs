using RotaryClubManager.Domain.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Domain.Entities
{
    public class Comite
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        // Relation avec le mandat
        public Guid MandatId { get; set; }
        public virtual Mandat Mandat { get; set; } = null!;
        // Relation avec le club
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;
    }


    public class ComiteMembre
    {
        public Guid Id { get; set; }
        // Relation avec le membre
        [Required]
        public string MembreId { get; set; } = string.Empty;
        public virtual ApplicationUser Membre { get; set; } = null!;

        // Relation avec le mandat
        public Guid MandatId { get; set; }
        public virtual Mandat Mandat { get; set; } = null!;

        // Relation avec la fonction
        [Required]
        public Guid FonctionId { get; set; }
        public virtual Fonction Fonction { get; set; } = null!;
    }

    public class Fonction
    {
        public Guid Id { get; set; }
        [Required]
        public string NomFonction { get; set; } = string.Empty;
    }

}
