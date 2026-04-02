using System;
using System.ComponentModel.DataAnnotations;

namespace MonAppGestion.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public int Qte { get; set; }
        public decimal PrixAchat { get; set; }
        public decimal PrixVente { get; set; }
        public DateTime? DateExpiration { get; set; }
    }
}
