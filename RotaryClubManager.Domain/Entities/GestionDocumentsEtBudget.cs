using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RotaryClubManager.Domain.Entities
{
    // ===== MODÈLES POUR LES DOCUMENTS =====

    public class Categorie
    {
        public Guid Id { get; set; }

        [Required]
        public string Libelle { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    public class TypeDocument
    {
        public Guid Id { get; set; }

        [Required]
        public string Libelle { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    public class Document
    {
        public Guid Id { get; set; }

        [Required]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public byte[] Fichier { get; set; } = Array.Empty<byte>();

        // Relations
        [Required]
        public Guid CategorieId { get; set; }
        public virtual Categorie Categorie { get; set; } = null!;

        [Required]
        public Guid TypeDocumentId { get; set; }
        public virtual TypeDocument TypeDocument { get; set; } = null!;

        [Required]
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;
    }

    // ===== MODÈLES POUR LE BUDGET =====

    public class TypeBudget
    {
        public Guid Id { get; set; }

        [Required]
        public string Libelle { get; set; } = string.Empty; // Dépenses, Recettes

        // Navigation properties
        public virtual ICollection<CategoryBudget> CategoriesBudget { get; set; } = new List<CategoryBudget>();
    }

    public class CategoryBudget
    {
        public Guid Id { get; set; }

        [Required]
        public string Libelle { get; set; } = string.Empty; // Fonctionnement, Caritatif

        // Relations
        [Required]
        public Guid TypeBudgetId { get; set; }
        public virtual TypeBudget TypeBudget { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<SousCategoryBudget> SousCategories { get; set; } = new List<SousCategoryBudget>();
    }

    public class SousCategoryBudget
    {
        public Guid Id { get; set; }

        [Required]
        public string Libelle { get; set; } = string.Empty;

        // Relations
        [Required]
        public Guid CategoryBudgetId { get; set; }
        public virtual CategoryBudget CategoryBudget { get; set; } = null!;

        [Required]
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<RubriqueBudget> Rubriques { get; set; } = new List<RubriqueBudget>();
    }

    public class RubriqueBudget
    {
        public Guid Id { get; set; }

        [Required]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        public int Quantite { get; set; } = 1;

        // Propriété calculée pour le montant total budgété
        [NotMapped]
        public decimal MontantTotal => PrixUnitaire * Quantite;

        // Propriété calculée pour le montant total budgété
        [Required]
        public decimal MontantRealise { get; set; }

        // Relations
        [Required]
        public Guid SousCategoryBudgetId { get; set; }
        public virtual SousCategoryBudget SousCategoryBudget { get; set; } = null!;

        [Required]
        public Guid MandatId { get; set; }
        public virtual Mandat Mandat { get; set; } = null!;

        [Required]
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<RubriqueBudgetRealise> Realisations { get; set; } = new List<RubriqueBudgetRealise>();
    }

    public class RubriqueBudgetRealise
    {
        public Guid Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }
        public string? Commentaires { get; set; }
        // Relations
        [Required]
        public Guid RubriqueBudgetId { get; set; }
        public virtual RubriqueBudget RubriqueBudget { get; set; } = null!;
    }
}