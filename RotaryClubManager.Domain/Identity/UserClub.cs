using RotaryClubManager.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Domain.Identity
{
    public class UserClub
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); // Ajout de la clé primaire

        public string UserId { get; set; } = string.Empty;
        public Guid ClubId { get; set; }
        public DateTime JoinedDate { get; set; }

        // Relations
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Club Club { get; set; } = null!;
    }
}
