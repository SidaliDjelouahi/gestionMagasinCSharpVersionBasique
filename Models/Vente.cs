using System;
using System.Collections.Generic;

namespace MonAppGestion.Models
{
    public class Vente
    {
        public int Id { get; set; }
        public string NumVente { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        // Nouveau : montant versé pour cette vente
        public decimal Versement { get; set; } = 0m;

        // Nouveau : référence au client (nullable)
        public int? IdClient { get; set; }
        public Client? Client { get; set; }

        public List<VenteDetail> Details { get; set; } = new List<VenteDetail>();
    }
}
