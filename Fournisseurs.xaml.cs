using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class Fournisseurs : Page
    {
        public Fournisseurs()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFournisseurs();
        }

        private void LoadFournisseurs()
        {
            using var db = new AppDbContext();
            var list = db.Fournisseurs.OrderBy(c => c.Id).ToList();
            dgFournisseurs.ItemsSource = list;
            txtStatus.Text = $"{list.Count} fournisseur(s)";
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
            var f = new Fournisseur { Nom = nom, Adresse = txtAdresse.Text?.Trim() ?? string.Empty, Telephone = txtTelephone.Text?.Trim() ?? string.Empty };
            db.Fournisseurs.Add(f);
            db.SaveChanges();
            ClearInputs();
            LoadFournisseurs();
        }

        private void btnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (dgFournisseurs.SelectedItem is not Fournisseur sel)
            {
                MessageBox.Show("Sélectionnez un fournisseur à modifier.", "Modifier", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var db = new AppDbContext();
            var f = db.Fournisseurs.Find(sel.Id);
            if (f == null) return;
            f.Nom = txtNom.Text?.Trim() ?? string.Empty;
            f.Adresse = txtAdresse.Text?.Trim() ?? string.Empty;
            f.Telephone = txtTelephone.Text?.Trim() ?? string.Empty;
            db.SaveChanges();
            LoadFournisseurs();
        }

        private void btnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (dgFournisseurs.SelectedItem is not Fournisseur sel)
            {
                MessageBox.Show("Sélectionnez un fournisseur à supprimer.", "Supprimer", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            using (var db = new AppDbContext())
            {
                // Vérifier si le fournisseur est référencé dans la table Achats
                var linked = db.Achats.Any(a => a.IdFournisseur == sel.Id);
                if (linked)
                {
                    MessageBox.Show("Impossible de supprimer : le fournisseur est lié à des achats.", "Suppression impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var res = MessageBox.Show($"Supprimer le fournisseur '{sel.Nom}' ?", "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                var f = db.Fournisseurs.Find(sel.Id);
                if (f == null) return;
                db.Fournisseurs.Remove(f);
                db.SaveChanges();
            }

            ClearInputs();
            LoadFournisseurs();
        }

        private void btnSituation_Click(object sender, RoutedEventArgs e)
        {
            if (dgFournisseurs.SelectedItem is not Fournisseur sel)
            {
                MessageBox.Show("Sélectionnez un fournisseur.", "Situation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                using var db = new AppDbContext();
                var achats = db.Achats.Where(a => a.IdFournisseur == sel.Id).Select(a => a.Id).ToList();
                if (!achats.Any())
                {
                    MessageBox.Show("Aucun achat pour ce fournisseur.", "Situation", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var details = db.AchatDetails.Where(d => achats.Contains(d.AchatId)).AsEnumerable().ToList();
                decimal total = details.Sum(d => d.PrixAchat * d.Qte);
                var versements = db.Achats.Where(a => a.IdFournisseur == sel.Id).AsEnumerable().Sum(a => a.Versement);
                var reste = total - versements;

                var wnd = new FournisseursSituation(sel.Id, sel.Nom);
                wnd.Owner = Window.GetWindow(this);
                wnd.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur lors du calcul : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgFournisseurs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFournisseurs.SelectedItem is Fournisseur sel)
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
