using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Management;
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
    // Ensure DB schema exists using code-first initializer
    DataInitializer.InitializeDatabase("database.db");

    using (var db = new AppDbContext())
    {
        // If no users, create default admin
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
        // Vérification du numéro de série du disque dur avant toute authentification
        var hddSn = GetHddSerial();
        if (hddSn != "NC8400R008422")
        {
            MessageBox.Show("La version a expire , veuillez contacter le fournisseur de l'application 0549466662", "Licence expirée", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using (var db = new AppDbContext())
        {
            // On cherche l'utilisateur dans la base
            var user = db.Users.FirstOrDefault(u => u.Username == txtUsername.Text && u.Password == txtPassword.Password);

           if (user != null)
            {
                // On crée l'objet Dashboard (attention à la majuscule)
                // store current user in session for role-based UI
                Session.CurrentUser = user;
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

    private string GetHddSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia");
            foreach (ManagementObject wmi in searcher.Get())
            {
                var sn = wmi["SerialNumber"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(sn))
                    return sn;
            }
        }
        catch
        {
            // ignore and return empty
        }
        return string.Empty;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        txtUsername.Focus();
        Keyboard.Focus(txtUsername);
    }
}