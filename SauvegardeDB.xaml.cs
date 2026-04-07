using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace MonAppGestion
{
    public partial class SauvegardeDB : Page
    {
        private readonly string _dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");

        public SauvegardeDB()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTables();
        }

        private void LoadTables()
        {
            // Controlled list of tables matching DbSet names
            // Do not list the detail tables separately; deletion of Ventes/Achats will remove details too
            var tables = new[] { "Users", "Products", "Ventes", "Clients", "Fournisseurs", "Achats" };
            cbTables.ItemsSource = tables;
        }

        private void BtnCopyDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog();
                dlg.Title = "Copier la base de données vers...";
                dlg.Filter = "SQLite DB (*.db)|*.db|All files (*.*)|*.*";
                dlg.FileName = $"database_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                    if (dlg.ShowDialog() == true)
                {
                    File.Copy(_dbFile, dlg.FileName, true);
                    txtCopyStatus.Text = "Copie réussie.";
                }
            }
            catch (Exception ex)
            {
                txtCopyStatus.Text = "Erreur: " + ex.Message;
            }
        }

        private void BtnImportDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog();
                dlg.Title = "Sélectionnez le fichier de base de données à importer";
                dlg.Filter = "SQLite DB (*.db)|*.db|All files (*.*)|*.*";
                if (dlg.ShowDialog() == true)
                {
                    // Make a backup first
                    var bak = _dbFile + ".preimport." + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak";
                    try { File.Copy(_dbFile, bak, true); } catch { }

                    try
                    {
                        File.Copy(dlg.FileName, _dbFile, true);
                        txtImportStatus.Text = "Import réussi.";
                    }
                    catch (IOException)
                    {
                        txtImportStatus.Text = "Erreur: impossible d'écraser le fichier. Fermez l'application et réessayez.";
                    }
                }
            }
            catch (Exception ex)
            {
                txtImportStatus.Text = "Erreur: " + ex.Message;
            }
        }

        private void BtnDeleteTable_Click(object sender, RoutedEventArgs e)
        {
            if (cbTables.SelectedItem == null)
            {
                txtDeleteStatus.Text = "Sélectionnez une table.";
                return;
            }

            var table = cbTables.SelectedItem?.ToString() ?? string.Empty;
            if (MessageBox.Show($"Confirmez la suppression de toutes les données de la table '{table}' ?", "Confirmer", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    // Special handling: when deleting Ventes or Achats, also clear their detail tables first
                    if (string.Equals(table, "Ventes", StringComparison.OrdinalIgnoreCase))
                    {
                        db.Database.ExecuteSqlRaw("DELETE FROM [VenteDetails]");
                        db.Database.ExecuteSqlRaw("DELETE FROM [Ventes]");
                    }
                    else if (string.Equals(table, "Achats", StringComparison.OrdinalIgnoreCase))
                    {
                        db.Database.ExecuteSqlRaw("DELETE FROM [AchatDetails]");
                        db.Database.ExecuteSqlRaw("DELETE FROM [Achats]");
                    }
                    else
                    {
                        // Generic delete for other tables (table name comes from a controlled list)
                        var sqlTable = table;
                        // avoid interpolated string to silence EF1002 warning; table name is from a controlled list
                        db.Database.ExecuteSqlRaw("DELETE FROM [" + sqlTable + "]");
                    }

                    txtDeleteStatus.Text = "Suppression effectuée.";
                }
            }
            catch (Exception ex)
            {
                txtDeleteStatus.Text = "Erreur: " + ex.Message;
            }
        }

        private void BtnImportProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog();
                dlg.Title = "Sélectionnez le fichier SQLite contenant la table Products";
                dlg.Filter = "SQLite DB (*.db)|*.db|All files (*.*)|*.*";
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                if (dlg.ShowDialog() != true) return;

                var sourceFile = dlg.FileName;
                if (!File.Exists(sourceFile))
                {
                    txtImportProductsStatus.Text = "Fichier introuvable.";
                    return;
                }

                // Read products from external DB using Microsoft.Data.Sqlite
                var productsToImport = new System.Collections.Generic.List<Product>();
                var cs = new SqliteConnectionStringBuilder { DataSource = sourceFile }.ToString();
                using (var conn = new SqliteConnection(cs))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Code, Nom, Qte, PrixAchat, PrixVente, DateExpiration FROM Products";
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                var p = new Product();
                                p.Code = rdr.IsDBNull(0) ? string.Empty : rdr.GetString(0);
                                p.Nom = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1);
                                // Safely parse Qte (can be stored as INTEGER (long), int, text, etc.)
                                p.Qte = 0;
                                if (!rdr.IsDBNull(2))
                                {
                                    var qobj = rdr.GetValue(2);
                                    long qlong = 0;
                                    if (qobj is long l) qlong = l;
                                    else if (qobj is int qi) qlong = qi;
                                    else if (qobj is double qd) qlong = (long)qd;
                                    else if (qobj is string qs && long.TryParse(qs, out var qparsed)) qlong = qparsed;
                                    else
                                    {
                                        try { qlong = Convert.ToInt64(qobj); } catch { qlong = 0; }
                                    }
                                    // clamp to Int32 range and non-negative
                                    if (qlong < 0) qlong = 0;
                                    if (qlong > int.MaxValue) qlong = int.MaxValue;
                                    p.Qte = (int)qlong;
                                }

                                // Safely parse prices (may be stored as REAL/double or text)
                                p.PrixAchat = 0m;
                                if (!rdr.IsDBNull(3))
                                {
                                    var o = rdr.GetValue(3);
                                    if (o is double od) p.PrixAchat = Convert.ToDecimal(od);
                                    else if (o is float of) p.PrixAchat = Convert.ToDecimal(of);
                                    else if (o is decimal dec) p.PrixAchat = dec;
                                    else if (o is long ol) p.PrixAchat = Convert.ToDecimal(ol);
                                    else if (o is string os && decimal.TryParse(os, NumberStyles.Any, CultureInfo.InvariantCulture, out var pd)) p.PrixAchat = pd;
                                    else { try { p.PrixAchat = Convert.ToDecimal(o); } catch { p.PrixAchat = 0m; } }
                                }

                                p.PrixVente = 0m;
                                if (!rdr.IsDBNull(4))
                                {
                                    var o2 = rdr.GetValue(4);
                                    if (o2 is double od2) p.PrixVente = Convert.ToDecimal(od2);
                                    else if (o2 is float of2) p.PrixVente = Convert.ToDecimal(of2);
                                    else if (o2 is decimal dec2) p.PrixVente = dec2;
                                    else if (o2 is long ol2) p.PrixVente = Convert.ToDecimal(ol2);
                                    else if (o2 is string os2 && decimal.TryParse(os2, NumberStyles.Any, CultureInfo.InvariantCulture, out var pv)) p.PrixVente = pv;
                                    else { try { p.PrixVente = Convert.ToDecimal(o2); } catch { p.PrixVente = 0m; } }
                                }
                                if (!rdr.IsDBNull(5))
                                {
                                    // try parse as text or datetime
                                    try { p.DateExpiration = rdr.GetDateTime(5); }
                                    catch
                                    {
                                        try
                                        {
                                            string? s = rdr.IsDBNull(5) ? null : rdr.GetValue(5)?.ToString();
                                            if (!string.IsNullOrWhiteSpace(s))
                                                p.DateExpiration = DateTime.Parse(s, CultureInfo.InvariantCulture);
                                            else
                                                p.DateExpiration = null;
                                        }
                                        catch { p.DateExpiration = null; }
                                    }
                                }
                                productsToImport.Add(p);
                            }
                        }
                    }
                }

                // Merge into current DB
                using (var db = new AppDbContext())
                {
                    int added = 0, updated = 0;
                    foreach (var ip in productsToImport)
                    {
                        Product? match = null;
                        if (!string.IsNullOrWhiteSpace(ip.Code))
                            match = db.Products.FirstOrDefault(x => x.Code == ip.Code);
                        if (match == null && !string.IsNullOrWhiteSpace(ip.Nom))
                            match = db.Products.FirstOrDefault(x => x.Nom == ip.Nom);

                        if (match != null)
                        {
                            // Update: aggregate quantity, and update prices if sensible
                            match.Qte += ip.Qte;
                            if (ip.PrixAchat > 0) match.PrixAchat = ip.PrixAchat;
                            if (ip.PrixVente > 0) match.PrixVente = ip.PrixVente;
                            if (ip.DateExpiration.HasValue) match.DateExpiration = ip.DateExpiration;
                            updated++;
                        }
                        else
                        {
                            // Insert new product (Id will be generated)
                            var np = new Product
                            {
                                Code = ip.Code ?? string.Empty,
                                Nom = ip.Nom ?? string.Empty,
                                Qte = ip.Qte,
                                PrixAchat = ip.PrixAchat,
                                PrixVente = ip.PrixVente,
                                DateExpiration = ip.DateExpiration
                            };
                            db.Products.Add(np);
                            added++;
                        }
                    }
                    db.SaveChanges();
                    txtImportProductsStatus.Text = $"Import terminé. Ajoutés: {added}, Mis à jour: {updated}.";
                }
            }
            catch (Exception ex)
            {
                txtImportProductsStatus.Text = "Erreur: " + ex.Message;
            }
        }
    }
}
