using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;
using MonAppGestion.Models; // Pour accéder à la classe User

namespace MonAppGestion;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
{
    InitializeComponent();
    SeedDatabase(); // On ajoute cette ligne
}

private void SeedDatabase()
{
    using (var db = new AppDbContext())
    {
            // Si la DB n'existe pas, la créer selon le modèle actuel.
            // Si elle existe mais manque la table Products, on recrée pour synchroniser le schéma.
            if (!File.Exists("database.db"))
            {
                db.Database.EnsureCreated();
            }
            else
            {
                try
                {
                    // Essaie d'accéder à la table Products ; si elle n'existe pas, une exception sera levée
                    var _ = db.Products.Any();
                }
                catch
                {
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();
                }
            }

        // Si aucun utilisateur n'existe, on crée l'admin
        if (!db.Users.Any())
        {
            db.Users.Add(new User 
            { 
                Username = "admin", 
                Password = "123", 
                Rank = "Admin" 
            });
            db.SaveChanges();
        }
    }
}
    private void btnLogin_Click(object sender, RoutedEventArgs e)
    {
        using (var db = new AppDbContext())
        {
            // On cherche l'utilisateur dans la base
            var user = db.Users.FirstOrDefault(u => u.Username == txtUsername.Text && u.Password == txtPassword.Password);

           if (user != null)
            {
                // On crée l'objet Dashboard (attention à la majuscule)
                Dashboard ds = new Dashboard();
                
                // On affiche la nouvelle fenêtre
                ds.Show();
                
                // On ferme la fenêtre de connexion actuelle
                this.Close();
            }
            else
            {
                lblMessage.Text = "Identifiants incorrects.";
}
            
        }
    }
}