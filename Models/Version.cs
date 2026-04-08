using System;

namespace MonAppGestion.Models
{
    public class Version
    {
        public int Id { get; set; }
        public string NomBranch { get; set; } = string.Empty;
        public string HDD_sn { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Client { get; set; } = string.Empty;
    }
}
