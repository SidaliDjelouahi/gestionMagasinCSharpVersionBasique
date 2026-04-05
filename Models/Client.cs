using System;
using System.ComponentModel.DataAnnotations;

namespace MonAppGestion.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
    }
}
