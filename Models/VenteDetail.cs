using System.ComponentModel.DataAnnotations.Schema;

namespace MonAppGestion.Models
{
    public class VenteDetail
    {
        public int Id { get; set; }

        [Column("IdVente")]
        public int VenteId { get; set; }

        [Column("IdProduit")]
        public int ProduitId { get; set; }

        public decimal PrixVente { get; set; }
        public int Qte { get; set; }
    }
}
