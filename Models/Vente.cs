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

        public List<VenteDetail> Details { get; set; } = new List<VenteDetail>();
    }
}
