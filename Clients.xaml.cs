using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class Clients : Page
    {
        public Clients()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadClients();
        }

        private void LoadClients()
        {
            using var db = new AppDbContext();
            var list = db.Clients.OrderBy(c => c.Id).ToList();
            dgClients.ItemsSource = list;
            txtStatus.Text = $"{list.Count} client(s)";
        }

        private void btnAjouter_Click(object sender, RoutedEventArgs e)
        {
            var nom = txtNom.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(nom))
            {
                MessageBox.Show("Le nom est requis.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var client = new Client { Nom = nom, Adresse = txtAdresse.Text?.Trim() ?? string.Empty, Telephone = txtTelephone.Text?.Trim() ?? string.Empty };
            db.Clients.Add(client);
            db.SaveChanges();
            ClearInputs();
            LoadClients();
        }

        private void btnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (dgClients.SelectedItem is not Client sel)
            {
                MessageBox.Show("Sélectionnez un client à modifier.", "Modifier", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var db = new AppDbContext();
            var client = db.Clients.Find(sel.Id);
            if (client == null) return;
            client.Nom = txtNom.Text?.Trim() ?? string.Empty;
            client.Adresse = txtAdresse.Text?.Trim() ?? string.Empty;
            client.Telephone = txtTelephone.Text?.Trim() ?? string.Empty;
            db.SaveChanges();
            LoadClients();
        }

        private void btnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (dgClients.SelectedItem is not Client sel)
            {
                MessageBox.Show("Sélectionnez un client à supprimer.", "Supprimer", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show($"Supprimer le client '{sel.Nom}' ?", "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var client = db.Clients.Find(sel.Id);
            if (client == null) return;
            db.Clients.Remove(client);
            db.SaveChanges();
            ClearInputs();
            LoadClients();
        }

        private void dgClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClients.SelectedItem is Client sel)
            {
                txtNom.Text = sel.Nom;
                txtAdresse.Text = sel.Adresse;
                txtTelephone.Text = sel.Telephone;
            }
        }

        private void ClearInputs()
        {
            txtNom.Text = string.Empty;
            txtAdresse.Text = string.Empty;
            txtTelephone.Text = string.Empty;
        }
    }
}
