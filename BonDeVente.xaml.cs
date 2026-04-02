using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;
using MonAppGestion.Models;
using Microsoft.EntityFrameworkCore;

namespace MonAppGestion
{
    public partial class BonDeVente : Page
    {
        private List<TempDetail> _lines = new List<TempDetail>();
        private List<Product> _allProducts = new List<Product>();
        private Product? _selectedProduct = null;

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
                txtProductSearch.Focus();
                Keyboard.Focus(txtProductSearch);

                // Compute next NumVente: take the maximum numeric NumVente and add 1
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var nums = db.Ventes.Select(v => v.NumVente).ToList();
                        var maxNum = 0;
                        foreach (var s in nums)
                        {
                            if (int.TryParse(s, out var v))
                            {
                                if (v > maxNum) maxNum = v;
                                continue;
                            }
                            // try to extract trailing digits
                            var digits = new string(s?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
                            if (int.TryParse(digits, out v))
                            {
                                if (v > maxNum) maxNum = v;
                            }
                        }
                        var next = maxNum + 1;
                        txtNumVente.Text = next.ToString();
                    }
                }
                catch { }
            }
            catch { }
        }

        private void ChargerProduits()
        {
            using (var db = new AppDbContext())
            {
                _allProducts = db.Products.ToList();
                lbProductSuggestions.ItemsSource = _allProducts;
            }
        }
        private void txtProductSearch_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                var text = txtProductSearch.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                {
                    lbProductSuggestions.Visibility = Visibility.Collapsed;
                    lbProductSuggestions.ItemsSource = null;
                    _selectedProduct = null;
                    return;
                }

                var filtered = _allProducts.Where(p =>
                    (!string.IsNullOrEmpty(p.Nom) && p.Nom.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(p.Code) && p.Code.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();

                if (filtered.Any())
                {
                    lbProductSuggestions.ItemsSource = filtered;
                    lbProductSuggestions.Visibility = Visibility.Visible;
                    // handle navigation keys
                    if (e.Key == Key.Enter)
                    {
                        // If there's an exact match by code or name, prefer it
                        Product? chosen = null;
                        var exact = filtered.FirstOrDefault(p => string.Equals(p.Nom, text, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(p.Code, text, StringComparison.OrdinalIgnoreCase));
                        chosen = exact ?? filtered.FirstOrDefault();
                        if (chosen != null)
                        {
                            // add directly to the bon with quantity=1 and prix=PrixVente
                            var line = new TempDetail
                            {
                                IdProduit = chosen.Id,
                                Nom = chosen.Nom,
                                PrixVente = chosen.PrixVente,
                                Qte = 1
                            };
                            _lines.Add(line);
                            RefreshDetailsGrid();
                            // clear search and hide suggestions
                            txtProductSearch.Clear();
                            lbProductSuggestions.Visibility = Visibility.Collapsed;
                            _selectedProduct = chosen;
                            // optionally set price and qty fields for user
                            txtPrixLine.Text = chosen.PrixVente.ToString();
                            txtQteLine.Text = "1";
                            // After adding a line, return focus to the product search box for quick next entry
                            txtProductSearch.Focus();
                            Keyboard.Focus(txtProductSearch);
                            e.Handled = true;
                        }
                    }
                }
                else
                {
                    lbProductSuggestions.ItemsSource = null;
                    lbProductSuggestions.Visibility = Visibility.Collapsed;
                    _selectedProduct = null;
                }
            }
            catch { }
        }

        private void txtProductSearch_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                txtProductSearch.Focus();
                Keyboard.Focus(txtProductSearch);
                if (lbProductSuggestions.Items.Count > 0)
                    lbProductSuggestions.Visibility = Visibility.Visible;
            }
            catch { }
        }

        private void txtProductSearch_KeyDown(object sender, KeyEventArgs e)
        {
            // If user presses Shift, move focus to the suggestions list (if any)
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                if (lbProductSuggestions.Items.Count > 0)
                {
                    lbProductSuggestions.Visibility = Visibility.Visible;
                    lbProductSuggestions.SelectedIndex = 0;
                    lbProductSuggestions.Focus();
                    Keyboard.Focus(lbProductSuggestions);
                    e.Handled = true;
                }
            }
        }

        private void lbProductSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (lbProductSuggestions.SelectedItem is Product sel)
                {
                    _selectedProduct = sel;
                    txtProductSearch.Text = sel.Nom;
                    lbProductSuggestions.Visibility = Visibility.Collapsed;
                }
            }
            catch { }
        }

        private void lbProductSuggestions_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    if (lbProductSuggestions.SelectedItem is Product sel)
                    {
                        _selectedProduct = sel;
                        txtProductSearch.Text = sel.Nom;
                        lbProductSuggestions.Visibility = Visibility.Collapsed;
                        // move focus to quantity textbox for quick entry
                        txtQteLine.Focus();
                        Keyboard.Focus(txtQteLine);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    lbProductSuggestions.Visibility = Visibility.Collapsed;
                    txtProductSearch.Focus();
                    Keyboard.Focus(txtProductSearch);
                    e.Handled = true;
                }
            }
            catch { }
        }

        private void btnAddLine_Click(object sender, RoutedEventArgs e)
        {
            Product? prod = null;
            if (lbProductSuggestions.SelectedItem is Product sel)
            {
                prod = sel;
            }
            else if (!string.IsNullOrWhiteSpace(txtProductSearch.Text))
            {
                prod = _allProducts.FirstOrDefault(p => string.Equals(p.Nom, txtProductSearch.Text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.Code, txtProductSearch.Text, StringComparison.OrdinalIgnoreCase));
            }

            if (prod != null &&
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
                // After adding a line via the button, return focus to the product search box
                txtProductSearch.Focus();
                Keyboard.Focus(txtProductSearch);
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

