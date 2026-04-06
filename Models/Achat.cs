using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonAppGestion.Models
{
    public class Achat
    {
        public int Id { get; set; }
        public string NumAchat { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        // reference to fournisseur (nullable) - use IdFournisseur as the FK column
        public int? IdFournisseur { get; set; }

        [ForeignKey("IdFournisseur")]
        public Fournisseur? Fournisseur { get; set; }

        // amount paid for the purchase
        public decimal Versement { get; set; } = 0m;

        public List<AchatDetail> Details { get; set; } = new List<AchatDetail>();
    }
}
