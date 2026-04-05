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
            using (var db = new AppDbContext())
            {
                // Vérifier si le client est référencé dans la table Ventes
                var linked = db.Ventes.Any(v => v.IdClient == sel.Id);
                if (linked)
                {
                    MessageBox.Show("Impossible de supprimer : le client est lié à des ventes.", "Suppression impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var res = MessageBox.Show($"Supprimer le client '{sel.Nom}' ?", "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                var client = db.Clients.Find(sel.Id);
                if (client == null) return;
                db.Clients.Remove(client);
                db.SaveChanges();
            }

            ClearInputs();
            LoadClients();
        }

        private void btnSituation_Click(object sender, RoutedEventArgs e)
        {
            if (dgClients.SelectedItem is not Client sel)
            {
                MessageBox.Show("Sélectionnez un client.", "Situation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                using var db = new AppDbContext();
                // Get ventes for client
                var ventes = db.Ventes.Where(v => v.IdClient == sel.Id).Select(v => v.Id).ToList();
                if (!ventes.Any())
                {
                    MessageBox.Show("Aucune vente pour ce client.", "Situation", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Load details into memory then compute sums to avoid provider translation issues
                var details = db.VenteDetails.Where(d => ventes.Contains(d.VenteId)).AsEnumerable().ToList();
                decimal total = details.Sum(d => d.PrixVente * d.Qte);
                var versements = db.Ventes.Where(v => v.IdClient == sel.Id).AsEnumerable().Sum(v => v.Versement);
                var reste = total - versements;

                var msg = $"Client: {sel.Nom}\nVentes: {ventes.Count}\nTotal TTC: {total:0.00}\nVersements: {versements:0.00}\nReste à payer: {reste:0.00}";
                    var wnd = new ClientsSituation(sel.Id, sel.Nom);
                    wnd.Owner = Window.GetWindow(this);
                    wnd.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur lors du calcul : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClients.SelectedItem is Client sel)
            {
                txtNom.Text = sel.Nom;
                txtAdresse.Text = sel.Adresse;
                txtTelephone.Text = sel.Telephone;
                btnModifier.IsEnabled = true;
                btnSupprimer.IsEnabled = true;
                btnSituation.IsEnabled = true;
            }
            else
            {
                btnModifier.IsEnabled = false;
                btnSupprimer.IsEnabled = false;
                btnSituation.IsEnabled = false;
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
