using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class Produits : Page
    {
        public Produits()
        {
            InitializeComponent();
            ChargerProduits();
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

            var prod = new Product
            {
                Code = txtCode.Text,
                Nom = txtNom.Text,
                Qte = qte,
                PrixAchat = prixA,
                PrixVente = prixV,
                DateExpiration = dpDateExp.SelectedDate
            };

            using (var db = new AppDbContext())
            {
                db.Products.Add(prod);
                db.SaveChanges();
            }

            txtCode.Clear(); txtNom.Clear(); txtQte.Clear(); txtPrixAchat.Clear(); txtPrixVente.Clear(); dpDateExp.SelectedDate = null;
            ChargerProduits();
        }

        private void btnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgProduits.SelectedItem as Product;
            if (selected == null)
            {
                MessageBox.Show("Sélectionnez d'abord un produit.");
                return;
            }

            if (MessageBox.Show($"Supprimer le produit {selected.Nom} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    // Attach if needed
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
