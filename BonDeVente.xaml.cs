using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MonAppGestion.Models;
using Microsoft.EntityFrameworkCore;

namespace MonAppGestion
{
    public partial class BonDeVente : Page
    {
        private List<TempDetail> _lines = new List<TempDetail>();

        public BonDeVente()
        {
            InitializeComponent();
            ChargerProduits();
            dpDateVente.SelectedDate = DateTime.Today;
            RefreshDetailsGrid();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dpDateVente.SelectedDate = DateTime.Today;
                cbProducts.Focus();
                Keyboard.Focus(cbProducts);
            }
            catch { }
        }

        private void ChargerProduits()
        {
            using (var db = new AppDbContext())
            {
                var produits = db.Products.ToList();
                cbProducts.ItemsSource = produits;
            }
        }

        private void btnAddLine_Click(object sender, RoutedEventArgs e)
        {
            if (cbProducts.SelectedItem is Product prod &&
                int.TryParse(txtQteLine.Text, out var qte) &&
                decimal.TryParse(txtPrixLine.Text, out var prix))
            {
                var line = new TempDetail
                {
                    IdProduit = prod.Id,
                    Nom = prod.Nom,
                    PrixVente = prix,
                    Qte = qte
                };
                _lines.Add(line);
                RefreshDetailsGrid();
                txtQteLine.Clear();
                txtPrixLine.Clear();
            }
            else
            {
                MessageBox.Show("Sélectionnez un produit et entrez une quantité et un prix valides.");
            }
        }

        private void RefreshDetailsGrid()
        {
            dgDetails.ItemsSource = null;
            dgDetails.ItemsSource = _lines.Select(l => new { l.Nom, l.PrixVente, l.Qte, Total = l.PrixVente * l.Qte }).ToList();
        }

        private void btnSaveVente_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNumVente.Text) || !dpDateVente.SelectedDate.HasValue)
            {
                MessageBox.Show("Remplissez le numéro et la date de la vente.");
                return;
            }

            if (!_lines.Any())
            {
                MessageBox.Show("Ajoutez au moins une ligne au bon.");
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var vente = new Vente
                    {
                        NumVente = txtNumVente.Text,
                        Date = dpDateVente.SelectedDate.Value
                    };
                    db.Ventes.Add(vente);
                    db.SaveChanges();

                    foreach (var l in _lines)
                    {
                        // Use raw SQL insert to avoid EF mapping issues in this environment
                        db.Database.ExecuteSqlInterpolated($"INSERT INTO VenteDetails (IdVente, IdProduit, PrixVente, Qte) VALUES ({vente.Id}, {l.IdProduit}, {l.PrixVente}, {l.Qte});");
                    }

                    // Read back saved details to confirm
                    var saved = db.VenteDetails.Where(d => d.VenteId == vente.Id).ToList();
                    MessageBox.Show($"Bon de vente enregistré. Lignes sauvegardées : {saved.Count}");
                }
            }
            catch (System.Exception ex)
            {
                try { System.IO.File.AppendAllText("crash.log", $"[BonDeVente.Save] {DateTime.Now}\n{ex}\n\n"); } catch { }
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message);
            }
            _lines.Clear();
            RefreshDetailsGrid();
            txtNumVente.Clear();
            dpDateVente.SelectedDate = null;
        }

        private class TempDetail
        {
            public int IdProduit { get; set; }
            public string Nom { get; set; } = string.Empty;
            public decimal PrixVente { get; set; }
            public int Qte { get; set; }
        }
    }
}

