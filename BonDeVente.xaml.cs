using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Globalization;
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
        private List<Client> _allClients = new List<Client>();
        private Product? _selectedProduct = null;
        private TempDetail? _editingLine = null;
        private bool _versementEdited = false;
        private bool _settingVersementProgrammatically = false;

        public BonDeVente()
        {
            InitializeComponent();
            ChargerProduits();
            ChargerClients();
            dpDateVente.SelectedDate = DateTime.Today;
            _versementEdited = false;
            _settingVersementProgrammatically = true;
            txtVersement.Text = "0.00";
            _settingVersementProgrammatically = false;
            RefreshDetailsGrid();
        }

        private void ChargerClients()
        {
            using (var db = new AppDbContext())
            {
                _allClients = db.Clients.OrderBy(c => c.Nom).ToList();
                cbClients.ItemsSource = _allClients;
            }
        }

        private void btnNewClient_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NewClientWindow();
            dlg.Owner = Window.GetWindow(this);
            var res = dlg.ShowDialog();
            if (res == true)
            {
                // reload clients and select newly created client
                ChargerClients();
                try { cbClients.SelectedValue = dlg.CreatedClientId; } catch { }
            }
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
                            // If product already exists in current lines, increment its quantity instead of adding a new line
                            var existing = _lines.FirstOrDefault(l => l.IdProduit == chosen.Id);
                            if (existing != null)
                            {
                                existing.Qte += 1;
                            }
                            else
                            {
                                var line = new TempDetail
                                {
                                    IdProduit = chosen.Id,
                                    Nom = chosen.Nom,
                                    PrixVente = chosen.PrixVente,
                                    Qte = 1
                                };
                                _lines.Add(line);
                            }
                            RefreshDetailsGrid();
                            // clear search and hide suggestions
                            txtProductSearch.Clear();
                            lbProductSuggestions.Visibility = Visibility.Collapsed;
                            _selectedProduct = chosen;
                            // optionally set price and qty fields for user
                            txtPrixLine.Text = chosen.PrixVente.ToString();
                            txtQteLine.Text = "1";
                            // After adding/incrementing, return focus to the product search box for quick next entry
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
                if (_editingLine != null)
                {
                    // Update existing line
                    _editingLine.PrixVente = prix;
                    _editingLine.Qte = qte;
                    _editingLine = null;
                    btnAddLine.Content = "Ajouter";
                }
                else
                {
                    // If a line for this product already exists, increment its quantity instead of adding a duplicate line
                    var existing = _lines.FirstOrDefault(l => l.IdProduit == prod.Id);
                    if (existing != null)
                    {
                        existing.Qte += qte;
                        // update price to the entered price (useful if price changed)
                        existing.PrixVente = prix;
                    }
                    else
                    {
                        var line = new TempDetail
                        {
                            IdProduit = prod.Id,
                            Nom = prod.Nom,
                            PrixVente = prix,
                            Qte = qte
                        };
                        _lines.Add(line);
                    }
                }
                RefreshDetailsGrid();
                txtQteLine.Clear();
                txtPrixLine.Clear();
                // After adding/updating, return focus to the product search box
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
            dgDetails.ItemsSource = _lines;
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            try
            {
                decimal total = _lines.Sum(l => l.Total);
                // show with two decimals
                txtTotal.Text = total.ToString("0.00");
                // If the user did not manually edit the versement, default it to the total
                if (!_versementEdited)
                {
                    try
                    {
                        _settingVersementProgrammatically = true;
                        txtVersement.Text = total.ToString("0.00");
                    }
                    finally { _settingVersementProgrammatically = false; }
                }
            }
            catch { txtTotal.Text = "0.00"; }
        }

        private void txtVersement_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_settingVersementProgrammatically) return;
            _versementEdited = true;
        }

        private void dgDetails_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            // Allow the edit to commit, then update total on the dispatcher
            try
            {
                Dispatcher.BeginInvoke((System.Action)(() => UpdateTotal()), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch { }
        }

        private void dgDetails_RowEditEnding(object sender, System.Windows.Controls.DataGridRowEditEndingEventArgs e)
        {
            try
            {
                Dispatcher.BeginInvoke((System.Action)(() => UpdateTotal()), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch { }
        }

        private void Action_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TempDetail td)
            {
                if (MessageBox.Show($"Supprimer la ligne '{td.Nom}' ?", "Confirmer", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _lines.Remove(td);
                    RefreshDetailsGrid();
                }
            }
            else if (sender is Button b && b.CommandParameter is TempDetail td2)
            {
                _lines.Remove(td2);
                RefreshDetailsGrid();
            }
        }

        private void Action_Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TempDetail td)
            {
                // populate the qty and price fields for editing
                _editingLine = td;
                txtQteLine.Text = td.Qte.ToString();
                txtPrixLine.Text = td.PrixVente.ToString();
                btnAddLine.Content = "Mettre à jour";
                // focus price for quick edit
                txtPrixLine.Focus();
                Keyboard.Focus(txtPrixLine);
            }
            else if (sender is Button b && b.CommandParameter is TempDetail td2)
            {
                _editingLine = td2;
                txtQteLine.Text = td2.Qte.ToString();
                txtPrixLine.Text = td2.PrixVente.ToString();
                btnAddLine.Content = "Mettre à jour";
                txtPrixLine.Focus();
                Keyboard.Focus(txtPrixLine);
            }
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
                    decimal versement = 0m;
                    // try current culture then invariant (accept both comma and dot)
                    if (!decimal.TryParse(txtVersement.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out versement))
                    {
                        var alt = txtVersement.Text?.Replace(',', '.');
                        decimal.TryParse(alt, NumberStyles.Number, CultureInfo.InvariantCulture, out versement);
                    }

                    var vente = new Vente
                    {
                        NumVente = txtNumVente.Text,
                        Date = dpDateVente.SelectedDate.Value,
                        Versement = versement
                    };
                    // attach selected client if any
                    if (cbClients.SelectedItem is Client selClient)
                    {
                        vente.IdClient = selClient.Id;
                    }
                    db.Ventes.Add(vente);
                    db.SaveChanges();

                    // ensure Versement persisted (fallback in case EF mapping issues)
                    try
                    {
                        db.Database.ExecuteSqlInterpolated($"UPDATE Ventes SET Versement = {versement}, IdClient = {vente.IdClient} WHERE Id = {vente.Id};");
                    }
                    catch { }

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
            // After saving, compute the next available NumVente (increment) and reset the date to today
            try
            {
                using (var db2 = new AppDbContext())
                {
                    var nums = db2.Ventes.Select(v => v.NumVente).ToList();
                    var maxNum = 0;
                    foreach (var s in nums)
                    {
                        if (int.TryParse(s, out var v))
                        {
                            if (v > maxNum) maxNum = v;
                            continue;
                        }
                        var digits = new string(s?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
                        if (int.TryParse(digits, out v))
                        {
                            if (v > maxNum) maxNum = v;
                        }
                    }
                    txtNumVente.Text = (maxNum + 1).ToString();
                }
            }
            catch { txtNumVente.Clear(); }
            dpDateVente.SelectedDate = DateTime.Today;
            try { txtVersement.Text = "0.00"; } catch { }
            try { txtQteLine.Clear(); txtPrixLine.Clear(); _editingLine = null; btnAddLine.Content = "Ajouter"; } catch { }
        }

        private class TempDetail : INotifyPropertyChanged
        {
            private int _idProduit;
            private string _nom = string.Empty;
            private decimal _prixVente;
            private int _qte;

            public int IdProduit
            {
                get => _idProduit;
                set { if (_idProduit != value) { _idProduit = value; OnPropertyChanged(nameof(IdProduit)); } }
            }

            public string Nom
            {
                get => _nom;
                set { if (_nom != value) { _nom = value; OnPropertyChanged(nameof(Nom)); } }
            }

            public decimal PrixVente
            {
                get => _prixVente;
                set
                {
                    if (_prixVente != value)
                    {
                        _prixVente = value;
                        OnPropertyChanged(nameof(PrixVente));
                        OnPropertyChanged(nameof(Total));
                    }
                }
            }

            public int Qte
            {
                get => _qte;
                set
                {
                    if (_qte != value)
                    {
                        _qte = value;
                        OnPropertyChanged(nameof(Qte));
                        OnPropertyChanged(nameof(Total));
                    }
                }
            }

            public decimal Total => PrixVente * Qte;

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

