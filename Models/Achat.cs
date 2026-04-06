using System;
using System.Collections.Generic;

namespace MonAppGestion.Models
{
    public class Achat
    {
        public int Id { get; set; }
        public string NumAchat { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        // reference to fournisseur (nullable)
        public int? IdFournisseur { get; set; }
        public Fournisseur? Fournisseur { get; set; }

        public List<AchatDetail> Details { get; set; } = new List<AchatDetail>();
    }
}
