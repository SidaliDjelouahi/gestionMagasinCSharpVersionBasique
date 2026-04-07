using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class Produits : Page
    {
        private int? _editingProductId = null;
        public bool ShowActions { get; set; } = true;

        public Produits()
        {
            InitializeComponent();
            Loaded += Produits_Loaded;
            ChargerProduits();
        }

        private void Produits_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                btnModifier.Visibility = ShowActions ? Visibility.Visible : Visibility.Collapsed;
                btnSupprimer.Visibility = ShowActions ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void ChargerProduits()
        {
            using (var db = new AppDbContext())
            {
                dgProduits.ItemsSource = db.Products.ToList();
            }
        }

        private void btnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtNom.Text))
            {
                MessageBox.Show("Remplissez au moins le code et le nom du produit.");
                return;
            }

            if (!int.TryParse(txtQte.Text, out var qte)) qte = 0;
            if (!decimal.TryParse(txtPrixAchat.Text, out var prixA)) prixA = 0;
            if (!decimal.TryParse(txtPrixVente.Text, out var prixV)) prixV = 0;

            using (var db = new AppDbContext())
            {
                if (_editingProductId.HasValue)
                {
                    var existing = db.Products.Find(_editingProductId.Value);
                    if (existing != null)
                    {
                        existing.Code = txtCode.Text;
                        existing.Nom = txtNom.Text;
                        existing.Qte = qte;
                        existing.PrixAchat = prixA;
                        existing.PrixVente = prixV;
                        existing.DateExpiration = dpDateExp.SelectedDate;
                        db.SaveChanges();
                    }
                    _editingProductId = null;
                    btnAjouter.Content = "Ajouter";
                }
                else
                {
                    var prod = new Product
                    {
                        Code = txtCode.Text,
                        Nom = txtNom.Text,
                        Qte = qte,
                        PrixAchat = prixA,
                        PrixVente = prixV,
                        DateExpiration = dpDateExp.SelectedDate
                    };
                    db.Products.Add(prod);
                    db.SaveChanges();
                }
            }

            txtCode.Clear(); txtNom.Clear(); txtQte.Clear(); txtPrixAchat.Clear(); txtPrixVente.Clear(); dpDateExp.SelectedDate = null;
            ChargerProduits();
        }

        private void btnModifier_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgProduits.SelectedItem as Product;
            if (selected == null)
            {
                MessageBox.Show("Sélectionnez d'abord un produit.");
                return;
            }

            txtCode.Text = selected.Code;
            txtNom.Text = selected.Nom;
            txtQte.Text = selected.Qte.ToString();
            txtPrixAchat.Text = selected.PrixAchat.ToString();
            txtPrixVente.Text = selected.PrixVente.ToString();
            dpDateExp.SelectedDate = selected.DateExpiration;

            _editingProductId = selected.Id;
            btnAjouter.Content = "Enregistrer";
        }

        private void btnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgProduits.SelectedItem as Product;
            if (selected == null)
            {
                MessageBox.Show("Sélectionnez d'abord un produit.");
                return;
            }
            // Check if product is referenced in VenteDetails (prevent deletion if used in sales)
            using (var db = new AppDbContext())
            {
                var isUsedInVentes = db.VenteDetails.Any(d => d.ProduitId == selected.Id);
                if (isUsedInVentes)
                {
                    MessageBox.Show($"Suppression refusée : le produit '{selected.Nom}' est présent dans des ventes (VenteDetails).", "Suppression refusée", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (MessageBox.Show($"Supprimer le produit {selected.Nom} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    var toRemove = db.Products.Find(selected.Id);
                    if (toRemove != null)
                    {
                        db.Products.Remove(toRemove);
                        db.SaveChanges();
                    }
                }
                ChargerProduits();
            }
        }
    }
}
