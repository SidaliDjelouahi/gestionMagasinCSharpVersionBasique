using Microsoft.EntityFrameworkCore;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<MonAppGestion.Models.Product> Products { get; set; }
        public DbSet<MonAppGestion.Models.Vente> Ventes { get; set; }
        public DbSet<MonAppGestion.Models.VenteDetail> VenteDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // Indique à l'application de créer le fichier database.db
            options.UseSqlite("Data Source=database.db");
        }
    }
}