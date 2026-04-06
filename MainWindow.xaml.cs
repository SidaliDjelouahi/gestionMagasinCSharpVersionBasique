using System.Text;
using System.Management;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
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

    // ----- Licence / Essai helpers -----
    private string GetHDDSerialNumber()
    {
        string sn = "";
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia");
            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                if (wmi_HD["SerialNumber"] != null)
                    sn = wmi_HD["SerialNumber"].ToString().Trim();
                break; // On prend le premier disque physique
            }
        }
        catch { }
        return sn;
    }

    private DateTime GetAccurateNow()
    {
        // Try to get network time, else fall back to local time
        try
        {
            var dt = GetNetworkTimeAsync().GetAwaiter().GetResult();
            if (dt != DateTime.MinValue) return dt;
        }
        catch { }
        return DateTime.Now;
    }

    private async Task<DateTime> GetNetworkTimeAsync()
    {
        try
        {
            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(5);
                using (var resp = await http.GetAsync("https://www.google.com").ConfigureAwait(false))
                {
                    if (resp.Headers.Date.HasValue)
                        return resp.Headers.Date.Value.UtcDateTime.ToLocalTime();
                }
            }
        }
        catch { }
        return DateTime.MinValue;
    }

    private byte[] ProtectBytes(byte[] data)
    {
        try { return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser); }
        catch { return null; }
    }

    private byte[] UnprotectBytes(byte[] data)
    {
        try { return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser); }
        catch { return null; }
    }

    private bool ValidateLicenseFile()
    {
        try
        {
            var licPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.lic");
            if (!File.Exists(licPath)) return false;
            var protectedBytes = File.ReadAllBytes(licPath);
            var bytes = UnprotectBytes(protectedBytes);
            if (bytes == null) return false;
            var content = Encoding.UTF8.GetString(bytes);
            // expected format: HDDSERIAL|ticks
            var parts = content.Split('|');
            if (parts.Length != 2) return false;
            var hdd = parts[0];
            if (!long.TryParse(parts[1], out var ticks)) return false;
            var expiry = new DateTime(ticks);
            if (DateTime.Now > expiry) return false;
            var currentHdd = GetHDDSerialNumber();
            return string.Equals(hdd, currentHdd, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private bool CheckTrial()
    {
        try
        {
            const string subKey = "Software\\MonAppGestion";
            const string valueName = "InstallDate";
            using (var key = Registry.CurrentUser.OpenSubKey(subKey, true) ?? Registry.CurrentUser.CreateSubKey(subKey))
            {
                var protectedValue = key.GetValue(valueName) as byte[];
                if (protectedValue == null)
                {
                    // first run, write protected install date
                    var now = GetAccurateNow();
                    var bytes = Encoding.UTF8.GetBytes(now.Ticks.ToString());
                    var toStore = ProtectBytes(bytes);
                    if (toStore != null) key.SetValue(valueName, toStore, RegistryValueKind.Binary);
                    return true; // trial active
                }
                var un = UnprotectBytes(protectedValue);
                if (un == null) return false;
                var s = Encoding.UTF8.GetString(un);
                if (!long.TryParse(s, out var ticks)) return false;
                var installDate = new DateTime(ticks);
                var nowLocal = GetAccurateNow();
                var days = (nowLocal - installDate).TotalDays;
                return days <= 30; // true = within trial
            }
        }
        catch { return false; }
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
        // Vérification licence / essai avant authentification
        var hasValidLicense = ValidateLicenseFile();
        if (!hasValidLicense)
        {
            var trialOk = CheckTrial();
            if (!trialOk)
            {
                MessageBox.Show("La période d'essai est expirée. Veuillez acheter une licence.", "Licence requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.IsEnabled = false;
                txtPassword.IsEnabled = false;
                var btn = sender as Button;
                if (btn != null) btn.IsEnabled = false;
                return;
            }
            else
            {
                lblMessage.Text = "Mode Essai actif.";
            }
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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        txtUsername.Focus();
        Keyboard.Focus(txtUsername);
        UpdateLicenseUI();
    }

    private void UpdateLicenseUI()
    {
        try
        {
            var hasValidLicense = ValidateLicenseFile();
            var trialOk = CheckTrial();
            if (!hasValidLicense && trialOk)
            {
                btnBuy.Visibility = Visibility.Visible;
                lblMessage.Text = "Mode Essai actif.";
            }
            else
            {
                btnBuy.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    private void btnBuy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Ouvrir page d'achat
            var psi = new ProcessStartInfo("https://example.com/acheter") { UseShellExecute = true };
            Process.Start(psi);
        }
        catch { }
    }
}