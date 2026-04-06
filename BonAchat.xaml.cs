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
using System.Printing;
using System.Windows.Documents;

namespace MonAppGestion
{
    public partial class BonAchat : Page
    {
        private List<TempDetail> _lines = new List<TempDetail>();
        private List<Product> _allProducts = new List<Product>();
        private List<Fournisseur> _allFournisseurs = new List<Fournisseur>();
        private Product? _selectedProduct = null;
        private TempDetail? _editingLine = null;
        private bool _versementEdited = false;
        private bool _settingVersementProgrammatically = false;

        public BonAchat()
        {
            InitializeComponent();
            ChargerProduits();
            ChargerFournisseurs();
            dpDateAchat.SelectedDate = DateTime.Today;
            _versementEdited = false;
            _settingVersementProgrammatically = true;
            txtVersement.Text = "0.00";
            _settingVersementProgrammatically = false;
            RefreshDetailsGrid();
        }

        private void ChargerFournisseurs()
        {
            using (var db = new AppDbContext())
            {
                _allFournisseurs = db.Fournisseurs.OrderBy(c => c.Nom).ToList();
                var placeholder = new Fournisseur { Id = 0, Nom = string.Empty, Adresse = string.Empty, Telephone = string.Empty };
                _allFournisseurs.Insert(0, placeholder);
                cbFournisseurs.ItemsSource = _allFournisseurs;
                cbFournisseurs.SelectedIndex = 0;
            }
        }

        private void btnNewFournisseur_Click(object sender, RoutedEventArgs e)
        {
            // Open Fournisseurs page in a dialog window for quick add
            var wnd = new Window { Title = "Fournisseurs", Width = 600, Height = 400, Owner = Window.GetWindow(this) };
            wnd.Content = new Fournisseurs();
            wnd.ShowDialog();
            // reload and attempt to keep selection
            ChargerFournisseurs();
        }

        private void btnNewProduct_Click(object sender, RoutedEventArgs e)
        {
            // Open Produits page in a dialog window for quick add
            var wnd = new Window { Title = "Produits", Width = 700, Height = 500, Owner = Window.GetWindow(this) };
            wnd.Content = new Produits();
            wnd.ShowDialog();
            // reload products and prefill the product search with the last added product if any
            ChargerProduits();
            try
            {
                var last = _allProducts.OrderByDescending(p => p.Id).FirstOrDefault();
                if (last != null)
                {
                    _selectedProduct = last;
                    txtProductSearch.Text = last.Nom;
                    txtPrixLine.Text = last.PrixAchat.ToString("0.00");
                    txtQteLine.Focus();
                    Keyboard.Focus(txtQteLine);
                }
            }
            catch { }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dpDateAchat.SelectedDate = DateTime.Today;
                txtProductSearch.Focus();
                Keyboard.Focus(txtProductSearch);

                try
                {
                    using (var db = new AppDbContext())
                    {
                        var nums = db.Achats.Select(v => v.NumAchat).ToList();
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
                        var next = maxNum + 1;
                        txtNumAchat.Text = next.ToString();
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
                    if (e.Key == Key.Enter)
                    {
                        Product? chosen = null;
                        var exact = filtered.FirstOrDefault(p => string.Equals(p.Nom, text, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(p.Code, text, StringComparison.OrdinalIgnoreCase));
                        chosen = exact ?? filtered.FirstOrDefault();
                        if (chosen != null)
                        {
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
                                    PrixAchat = chosen.PrixAchat,
                                    Qte = 1
                                };
                                _lines.Add(line);
                            }
                            RefreshDetailsGrid();
                            txtProductSearch.Clear();
                            lbProductSuggestions.Visibility = Visibility.Collapsed;
                            _selectedProduct = chosen;
                            // leave quantity and price inputs empty for manual entry
                            try { txtPrixLine.Clear(); } catch { }
                            try { txtQteLine.Clear(); } catch { }
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
                    _editingLine.PrixAchat = prix;
                    _editingLine.Qte = qte;
                    _editingLine = null;
                    btnAddLine.Content = "Ajouter";
                }
                else
                {
                    var existing = _lines.FirstOrDefault(l => l.IdProduit == prod.Id);
                    if (existing != null)
                    {
                        existing.Qte += qte;
                        existing.PrixAchat = prix;
                    }
                    else
                    {
                        var line = new TempDetail
                        {
                            IdProduit = prod.Id,
                            Nom = prod.Nom,
                            PrixAchat = prix,
                            Qte = qte
                        };
                        _lines.Add(line);
                    }
                }
                RefreshDetailsGrid();
                txtQteLine.Clear();
                txtPrixLine.Clear();
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
                txtTotal.Text = total.ToString("0.00");
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
        }

        private void Action_Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TempDetail td)
            {
                _editingLine = td;
                txtQteLine.Text = td.Qte.ToString();
                txtPrixLine.Text = td.PrixAchat.ToString();
                btnAddLine.Content = "Mettre à jour";
                txtPrixLine.Focus();
                Keyboard.Focus(txtPrixLine);
            }
        }

        private void btnSaveAchat_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNumAchat.Text) || !dpDateAchat.SelectedDate.HasValue)
            {
                MessageBox.Show("Remplissez le numéro et la date de l'achat.");
                return;
            }

            if (!_lines.Any())
            {
                MessageBox.Show("Ajoutez au moins une ligne à l'achat.");
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    decimal versement = 0m;
                    if (!decimal.TryParse(txtVersement.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out versement))
                    {
                        var alt = txtVersement.Text?.Replace(',', '.');
                        decimal.TryParse(alt, NumberStyles.Number, CultureInfo.InvariantCulture, out versement);
                    }

                    var achat = new Achat
                    {
                        NumAchat = txtNumAchat.Text,
                        Date = dpDateAchat.SelectedDate.Value,
                        Versement = versement
                    };
                    if (cbFournisseurs.SelectedItem is Fournisseur selF && selF.Id != 0)
                    {
                        achat.IdFournisseur = selF.Id;
                    }
                    db.Achats.Add(achat);
                    db.SaveChanges();

                    try
                    {
                        db.Database.ExecuteSqlInterpolated($"UPDATE Achats SET Versement = {versement}, IdFournisseur = {achat.IdFournisseur} WHERE Id = {achat.Id};");
                    }
                    catch { }

                    foreach (var l in _lines)
                    {
                        // update product stock
                        var prod = db.Products.Find(l.IdProduit);
                        if (prod != null)
                        {
                            prod.Qte += l.Qte;
                        }

                        db.Database.ExecuteSqlInterpolated($"INSERT INTO AchatDetails (IdAchat, IdProduit, PrixAchat, Qte) VALUES ({achat.Id}, {l.IdProduit}, {l.PrixAchat}, {l.Qte});");
                    }

                    db.SaveChanges();

                    var saved = db.AchatDetails.Where(d => d.AchatId == achat.Id).ToList();
                    MessageBox.Show($"Achat enregistré. Lignes sauvegardées : {saved.Count}");
                }
            }
            catch (System.Exception ex)
            {
                try { System.IO.File.AppendAllText("crash.log", $"[BonAchat.Save] {DateTime.Now}\n{ex}\n\n"); } catch { }
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message);
            }

            _lines.Clear();
            RefreshDetailsGrid();
            try
            {
                using (var db2 = new AppDbContext())
                {
                    var nums = db2.Achats.Select(v => v.NumAchat).ToList();
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
                    txtNumAchat.Text = (maxNum + 1).ToString();
                }
            }
            catch { txtNumAchat.Clear(); }
            dpDateAchat.SelectedDate = DateTime.Today;
            try { txtVersement.Text = "0.00"; } catch { }
            try { txtQteLine.Clear(); txtPrixLine.Clear(); _editingLine = null; btnAddLine.Content = "Ajouter"; } catch { }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!_lines.Any())
            {
                MessageBox.Show("Aucune ligne à imprimer.", "Imprimer", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var fd = new FlowDocument();
            fd.PagePadding = new Thickness(12);
            fd.ColumnWidth = 300;

            var header = new Paragraph(new Bold(new Run("Bon d'achat"))) { FontSize = 16, TextAlignment = TextAlignment.Center };
            fd.Blocks.Add(header);

            var meta = new Paragraph();
            meta.Inlines.Add(new Run($"N°: {txtNumAchat.Text}") { FontWeight = FontWeights.Bold });
            meta.Inlines.Add(new LineBreak());
            meta.Inlines.Add(new Run($"Date: {dpDateAchat.SelectedDate?.ToString("g") ?? string.Empty}"));
            meta.Inlines.Add(new LineBreak());
            if (cbFournisseurs.SelectedItem is Fournisseur c && c.Id != 0)
            {
                meta.Inlines.Add(new Run($"Fournisseur: {c.Nom}"));
            }
            fd.Blocks.Add(meta);

            var table = new Table();
            table.CellSpacing = 0;
            table.Columns.Add(new TableColumn() { Width = new GridLength(140) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(50) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(60) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(60) });

            var rowGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Produit")))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Qte")))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Prix")))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Total")))));
            rowGroup.Rows.Add(headerRow);

            foreach (var l in _lines)
            {
                var r = new TableRow();
                r.Cells.Add(new TableCell(new Paragraph(new Run(l.Nom))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(l.Qte.ToString()))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(l.PrixAchat.ToString("0.00")))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(l.Total.ToString("0.00")))));
                rowGroup.Rows.Add(r);
            }

            table.RowGroups.Add(rowGroup);
            fd.Blocks.Add(table);

            var totals = new Paragraph();
            totals.TextAlignment = TextAlignment.Right;
            totals.Inlines.Add(new Run($"Total: {txtTotal.Text}") { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new LineBreak());
            totals.Inlines.Add(new Run($"Versement: {txtVersement.Text}"));
            try
            {
                decimal total = decimal.Parse(txtTotal.Text);
                decimal versement = 0m;
                decimal.TryParse(txtVersement.Text, out versement);
                totals.Inlines.Add(new LineBreak());
                totals.Inlines.Add(new Run($"Reste: {(total - versement):0.00}") { FontWeight = FontWeights.Bold });
            }
            catch { }
            fd.Blocks.Add(totals);

            fd.Blocks.Add(new Paragraph(new Run("Merci.")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0,12,0,0) });

            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                IDocumentPaginatorSource idp = fd;
                try
                {
                    pd.PrintDocument(idp.DocumentPaginator, "BonAchat");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur impression: " + ex.Message);
                }
            }
        }

        private class TempDetail : INotifyPropertyChanged
        {
            private int _idProduit;
            private string _nom = string.Empty;
            private decimal _prixAchat;
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

            public decimal PrixAchat
            {
                get => _prixAchat;
                set
                {
                    if (_prixAchat != value)
                    {
                        _prixAchat = value;
                        OnPropertyChanged(nameof(PrixAchat));
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

            public decimal Total => PrixAchat * Qte;

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
