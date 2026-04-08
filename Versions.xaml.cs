using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class Versions : Page
    {
        private int? _editingId = null;
        public Versions()
        {
            InitializeComponent();
            Loaded += Versions_Loaded;
            ChargerVersions();
        }

        private void Versions_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                // ensure actions visible only for developers; Dashboard already hides the nav button,
                // but double-check user's role here
                var role = Session.CurrentUser?.Rank?.ToLowerInvariant() ?? string.Empty;
                var allowActions = role == "developpeur" || role == "admin";
                btnModifierVersion.Visibility = allowActions ? Visibility.Visible : Visibility.Collapsed;
                btnSupprimerVersion.Visibility = allowActions ? Visibility.Visible : Visibility.Collapsed;
                btnAjouterVersion.Visibility = allowActions ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void ChargerVersions()
        {
            using (var db = new AppDbContext())
            {
                dgVersions.ItemsSource = db.Versions.OrderByDescending(v => v.Date).ToList();
            }
        }

        private void btnAjouterVersion_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNomBranch.Text))
            {
                MessageBox.Show("Nom de la branche requis.");
                return;
            }

            using (var db = new AppDbContext())
            {
                if (_editingId.HasValue)
                {
                    var existing = db.Versions.Find(_editingId.Value);
                    if (existing != null)
                    {
                        existing.NomBranch = txtNomBranch.Text;
                        existing.HDD_sn = txtHddSn.Text;
                        existing.Client = txtClient.Text;
                        existing.Date = DateTime.Now;
                        db.SaveChanges();
                    }
                    _editingId = null;
                    btnAjouterVersion.Content = "Ajouter";
                }
                else
                {
                    var v = new MonAppGestion.Models.Version
                    {
                        NomBranch = txtNomBranch.Text,
                        HDD_sn = txtHddSn.Text,
                        Client = txtClient.Text,
                        Date = DateTime.Now
                    };
                    db.Versions.Add(v);
                    db.SaveChanges();
                }
            }

            txtNomBranch.Clear(); txtHddSn.Clear(); txtClient.Clear();
            ChargerVersions();
        }

        private void btnModifierVersion_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgVersions.SelectedItem as MonAppGestion.Models.Version;
            if (selected == null)
            {
                MessageBox.Show("Sélectionnez d'abord une version.");
                return;
            }

            txtNomBranch.Text = selected.NomBranch;
            txtHddSn.Text = selected.HDD_sn;
            txtClient.Text = selected.Client;
            _editingId = selected.Id;
            btnAjouterVersion.Content = "Enregistrer";
        }

        private void btnSupprimerVersion_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgVersions.SelectedItem as MonAppGestion.Models.Version;
            if (selected == null)
            {
                MessageBox.Show("Sélectionnez d'abord une version.");
                return;
            }

            if (MessageBox.Show($"Supprimer la version '{selected.NomBranch}' ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    var toRemove = db.Versions.Find(selected.Id);
                    if (toRemove != null)
                    {
                        db.Versions.Remove(toRemove);
                        db.SaveChanges();
                    }
                }
                ChargerVersions();
            }
        }
    }
}
