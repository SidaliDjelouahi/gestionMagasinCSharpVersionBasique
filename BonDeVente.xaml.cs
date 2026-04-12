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
    public partial class BonDeVente : Page
    {
        private List<TempDetail> _lines = new List<TempDetail>();
        private List<Product> _allProducts = new List<Product>();
        private List<Client> _allClients = new List<Client>();
        private List<Product> _shortcutProducts = new List<Product>();
        private Product? _selectedProduct = null;
        private TempDetail? _editingLine = null;
        private bool _versementEdited = false;
        private bool _settingVersementProgrammatically = false;

        // Buffer douchette (scan rapide)
        private string _barcodeBuffer = "";
        private DateTime _lastBarcodeKeystroke = DateTime.Now;
        private const int BarcodeDelay = 50; // ms

        public BonDeVente()
        {
            InitializeComponent();
            ChargerProduits();
            ChargerClients();
            dpDateVente.SelectedDate = DateTime.Today;
            try { txtTime.Text = DateTime.Now.ToString("HH:mm"); } catch { }
            _versementEdited = false;
            _settingVersementProgrammatically = true;
            txtVersement.Text = "0.00";
            _settingVersementProgrammatically = false;
            RefreshDetailsGrid();
            // register F6 handler and barcode handler on parent Window to allow global capture
            this.Loaded += (s, e) =>
            {
                var w = Window.GetWindow(this);
                if (w != null)
                {
                    w.PreviewKeyDown += Window_PreviewKeyDown;
                    w.PreviewKeyDown += BonDeVente_PreviewKeyDown_Barcode;
                }
            };
        }

        // Gestion du buffer douchette (scan rapide)
        private void BonDeVente_PreviewKeyDown_Barcode(object? sender, KeyEventArgs e)
        {
            // Si le focus est dans une TextBox d'édition (hors txtProductSearch), on ignore
            if (Keyboard.FocusedElement is TextBox tb && tb != txtProductSearch)
                return;

            // On ne traite que les touches numériques et Enter
            if (e.Key == Key.Enter)
            {
                if (_barcodeBuffer.Length > 0)
                {
                    string code = _barcodeBuffer;
                    _barcodeBuffer = "";
                    Dispatcher.Invoke(() => TraiterCodeBarre(code));
                    e.Handled = true;
                }
                return;
            }

            TimeSpan elapsed = DateTime.Now - _lastBarcodeKeystroke;
            if (elapsed.TotalMilliseconds > BarcodeDelay)
                _barcodeBuffer = ""; // Trop lent, on considère que c'est un humain

            // Ajout des chiffres (clavier principal)
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
                _barcodeBuffer += (char)('0' + (e.Key - Key.D0));
            // Ajout des chiffres (pavé numérique)
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                _barcodeBuffer += (char)('0' + (e.Key - Key.NumPad0));
            // Ajoutez d'autres touches si besoin (lettres, etc.)

            _lastBarcodeKeystroke = DateTime.Now;
        }

        // Traitement du code-barres scanné
        private void TraiterCodeBarre(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            // Recherche du produit par code
            var chosen = _allProducts.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
            if (chosen != null)
            {
                var existing = _lines.FirstOrDefault(l => l.IdProduit == chosen.Id);
                if (existing != null)
                    existing.Qte += 1;
                else
                    _lines.Add(new TempDetail { IdProduit = chosen.Id, Nom = chosen.Nom, PrixVente = chosen.PrixVente, Qte = 1 });
                RefreshDetailsGrid();
                // Efface la recherche et suggestions
                txtProductSearch.Clear();
                lbProductSuggestions.Visibility = Visibility.Collapsed;
                _selectedProduct = chosen;
                try { txtPrixLine.Clear(); } catch { }
                try { txtQteLine.Clear(); } catch { }
                txtProductSearch.Focus();
                Keyboard.Focus(txtProductSearch);
            }
            else
            {
                // Optionnel : afficher un message si le code n'est pas trouvé
                MessageBox.Show($"Produit non trouvé pour le code : {code}");
            }
        }

        // helper to get the selected date+time from the controls
        private DateTime GetSelectedDateTime()
        {
            try
            {
                var date = dpDateVente.SelectedDate ?? DateTime.Today;
                var timeText = (txtTime?.Text ?? string.Empty).Trim();
                if (TimeSpan.TryParse(timeText, out var ts))
                {
                    return date.Date + ts;
                }
                // try parse as DateTime for flexible formats
                if (DateTime.TryParse(timeText, out var dt))
                {
                    return date.Date + dt.TimeOfDay;
                }
            }
            catch { }
            return dpDateVente.SelectedDate ?? DateTime.Today;
        }

        private void Window_PreviewKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.F6)
                {
                    if (!_lines.Any())
                    {
                        MessageBox.Show("Aucune ligne à modifier.");
                        e.Handled = true;
                        return;
                    }

                    var last = _lines.Last();
                    var dlg = new EditLastLineWindow(last.Nom, last.Qte, last.PrixVente);
                    dlg.Owner = Window.GetWindow(this);
                    var res = dlg.ShowDialog();
                    if (res == true)
                    {
                        // apply changes to the last line
                        last.Nom = dlg.ProductName ?? last.Nom;
                        last.Qte = dlg.Qty;
                        last.PrixVente = dlg.Price;
                        RefreshDetailsGrid();
                    }
                    e.Handled = true;
                }
            }
            catch { }
        }

        private void ChargerClients()
        {
            using (var db = new AppDbContext())
            {
                _allClients = db.Clients.OrderBy(c => c.Nom).ToList();
                // Insert an empty placeholder so the ComboBox shows a blank entry before any selection
                var placeholder = new Client { Id = 0, Nom = string.Empty, Adresse = string.Empty, Telephone = string.Empty };
                _allClients.Insert(0, placeholder);
                cbClients.ItemsSource = _allClients;
                // select the placeholder by default
                cbClients.SelectedIndex = 0;
                try { txtClientName.Text = string.Empty; } catch { }
            }
        }

        private void cbClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cbClients.SelectedItem is Client c && c.Id != 0)
                {
                    txtClientName.Text = c.Nom;
                }
                else
                {
                    txtClientName.Text = string.Empty;
                }
            }
            catch { }
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
                _shortcutProducts = db.Products.Where(p => p.Raccourci > 0).ToList();
                RefreshShortcuts();
            }
        }
        // Ajout douchette : si saisie rapide (scanne), ajouter direct la ligne
        private DateTime _lastProductSearchInput = DateTime.MinValue;
        private string _lastProductSearchText = string.Empty;
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
            try
            {
                // Validation douchette ou manuelle : uniquement sur Enter
                if (e.Key == Key.Enter)
                {
                    var text = txtProductSearch.Text ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var chosen = _allProducts.FirstOrDefault(p => string.Equals(p.Code, text, StringComparison.OrdinalIgnoreCase))
                            ?? _allProducts.FirstOrDefault(p => string.Equals(p.Nom, text, StringComparison.OrdinalIgnoreCase));
                        if (chosen != null)
                        {
                            var existing = _lines.FirstOrDefault(l => l.IdProduit == chosen.Id);
                            if (existing != null)
                                existing.Qte += 1;
                            else
                                _lines.Add(new TempDetail { IdProduit = chosen.Id, Nom = chosen.Nom, PrixVente = chosen.PrixVente, Qte = 1 });
                            RefreshDetailsGrid();
                        }
                        // clear search and hide suggestions
                        txtProductSearch.Clear();
                        lbProductSuggestions.Visibility = Visibility.Collapsed;
                        _selectedProduct = chosen;
                        try { txtPrixLine.Clear(); } catch { }
                        try { txtQteLine.Clear(); } catch { }
                        txtProductSearch.Focus();
                        Keyboard.Focus(txtProductSearch);
                        e.Handled = true;
                        return;
                    }
                }
                // If user presses Escape, clear search and keep focus on the textbox
                if (e.Key == Key.Escape)
                {
                    try { txtProductSearch.Clear(); } catch { }
                    lbProductSuggestions.Visibility = Visibility.Collapsed;
                    _selectedProduct = null;
                    txtProductSearch.Focus();
                    Keyboard.Focus(txtProductSearch);
                    e.Handled = true;
                    return;
                }
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
            catch { }
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

        private void btnCreateShortcut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Product? prod = null;
                if (lbProductSuggestions.SelectedItem is Product sel)
                    prod = sel;
                else if (!string.IsNullOrWhiteSpace(txtProductSearch.Text))
                    prod = _allProducts.FirstOrDefault(p => string.Equals(p.Nom, txtProductSearch.Text, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(p.Code, txtProductSearch.Text, StringComparison.OrdinalIgnoreCase));

                if (prod == null)
                {
                    MessageBox.Show("Sélectionnez un produit valide pour créer un raccourci.");
                    return;
                }

                if (_shortcutProducts.Any(p => p.Id == prod.Id))
                {
                    MessageBox.Show("Ce produit est déjà en raccourci.");
                    return;
                }

                // Persist shortcut flag to DB
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var toUpdate = db.Products.Find(prod.Id);
                        if (toUpdate != null)
                        {
                            toUpdate.Raccourci = 1;
                            db.SaveChanges();
                        }
                    }
                }
                catch { }

                _shortcutProducts.Add(prod);
                RefreshShortcuts();
            }
            catch { }
        }

        private void RefreshShortcuts()
        {
            try
            {
                spShortcuts.Children.Clear();
                foreach (var p in _shortcutProducts)
                {
                    var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) };
                    var b = new Button { Content = p.Nom, Width = 80, Tag = p.Id };
                    b.Click += ShortcutButton_Click;
                    var remove = new Button { Content = "x", Width = 24, Height = 24, Margin = new Thickness(4,0,0,0), Tag = p.Id };
                    remove.Click += RemoveShortcut_Click;
                    container.Children.Add(b);
                    container.Children.Add(remove);
                    spShortcuts.Children.Add(container);
                }
            }
            catch { }
        }

        private void RemoveShortcut_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button b && b.Tag is int id)
                {
                    // update DB to set Raccourci = null
                    try
                    {
                        using (var db = new AppDbContext())
                        {
                            var prod = db.Products.Find(id);
                            if (prod != null)
                            {
                                prod.Raccourci = null;
                                db.SaveChanges();
                            }
                        }
                    }
                    catch { }

                    _shortcutProducts.RemoveAll(p => p.Id == id);
                    RefreshShortcuts();
                }
            }
            catch { }
        }

        private void ShortcutButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button b && b.Tag is int id)
                {
                    var prod = _allProducts.FirstOrDefault(p => p.Id == id);
                    if (prod == null) return;
                    var existing = _lines.FirstOrDefault(l => l.IdProduit == prod.Id);
                    if (existing != null)
                    {
                        existing.Qte += 1;
                    }
                    else
                    {
                        var line = new TempDetail
                        {
                            IdProduit = prod.Id,
                            Nom = prod.Nom,
                            PrixVente = prod.PrixVente,
                            Qte = 1
                        };
                        _lines.Add(line);
                    }
                    RefreshDetailsGrid();
                }
            }
            catch { }
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
                        Date = GetSelectedDateTime(),
                        Versement = versement
                    };
                    // attach selected client if any
                    if (cbClients.SelectedItem is Client selClient && selClient.Id != 0)
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

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!_lines.Any())
            {
                MessageBox.Show("Aucune ligne à imprimer.", "Imprimer", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Build FlowDocument ticket
            var fd = new FlowDocument();
            fd.PagePadding = new Thickness(12);
            fd.ColumnWidth = 300; // small width for ticket printers

            // Header
            var header = new Paragraph(new Bold(new Run("Bon de vente"))) { FontSize = 16, TextAlignment = TextAlignment.Center };
            fd.Blocks.Add(header);

            // Sale meta
            var meta = new Paragraph();
            meta.Inlines.Add(new Run($"N°: {txtNumVente.Text}") { FontWeight = FontWeights.Bold });
            meta.Inlines.Add(new LineBreak());
            var selDt = GetSelectedDateTime();
            meta.Inlines.Add(new Run($"Date: {selDt.ToString("g")}"));
            meta.Inlines.Add(new LineBreak());
            if (cbClients.SelectedItem is MonAppGestion.Models.Client c && c.Id != 0)
            {
                meta.Inlines.Add(new Run($"Client: {c.Nom}"));
            }
            fd.Blocks.Add(meta);

            // Table of products
            var table = new Table();
            table.CellSpacing = 0;
            table.Columns.Add(new TableColumn() { Width = new GridLength(140) }); // name
            table.Columns.Add(new TableColumn() { Width = new GridLength(50) }); // qty
            table.Columns.Add(new TableColumn() { Width = new GridLength(60) }); // price
            table.Columns.Add(new TableColumn() { Width = new GridLength(60) }); // total

            var rowGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Produit"))))) ;
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Qte"))))) ;
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Prix"))))) ;
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Total"))))) ;
            rowGroup.Rows.Add(headerRow);

            foreach (var l in _lines)
            {
                var r = new TableRow();
                r.Cells.Add(new TableCell(new Paragraph(new Run(l.Nom))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(l.Qte.ToString()))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(l.PrixVente.ToString("0.00")))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(l.Total.ToString("0.00")))));
                rowGroup.Rows.Add(r);
            }

            table.RowGroups.Add(rowGroup);
            fd.Blocks.Add(table);

            // Totals
            var totals = new Paragraph();
            totals.TextAlignment = TextAlignment.Right;
            totals.Inlines.Add(new Run($"Total: {txtTotal.Text}") { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new LineBreak());
            totals.Inlines.Add(new Run($"Versement: {txtVersement.Text}"));
            // Compute situation (remainder) if possible
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

            // Footer
            fd.Blocks.Add(new Paragraph(new Run("Merci pour votre achat")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0,12,0,0) });

            // Print
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                // Use DocumentPaginator to print FlowDocument
                IDocumentPaginatorSource idp = fd;
                try
                {
                    pd.PrintDocument(idp.DocumentPaginator, "BonDeVente");
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

