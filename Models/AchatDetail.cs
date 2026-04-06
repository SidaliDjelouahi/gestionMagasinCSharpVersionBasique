using System.ComponentModel.DataAnnotations.Schema;

namespace MonAppGestion.Models
{
    public class AchatDetail
    {
        public int Id { get; set; }

        [Column("IdAchat")]
        public int AchatId { get; set; }

        [Column("IdProduit")]
        public int ProduitId { get; set; }

        public decimal PrixAchat { get; set; }
        public int Qte { get; set; }
    }
}
