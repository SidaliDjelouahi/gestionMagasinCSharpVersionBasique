using System;
using System.Collections.Generic;

namespace MonAppGestion.Models
{
    public class Vente
    {
        public int Id { get; set; }
        public string NumVente { get; set; }
        public DateTime Date { get; set; }

        public List<VenteDetail> Details { get; set; }
    }
}
