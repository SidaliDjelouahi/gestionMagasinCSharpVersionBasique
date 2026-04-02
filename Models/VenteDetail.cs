namespace MonAppGestion.Models
{
    public class VenteDetail
    {
        public int Id { get; set; }
        public int IdVente { get; set; }
        public int IdProduit { get; set; }
        public decimal PrixVente { get; set; }
        public int Qte { get; set; }
    }
}
