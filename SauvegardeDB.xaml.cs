using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

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
                        // Generic delete for other tables
                        var sqlTable = table;
                        db.Database.ExecuteSqlRaw($"DELETE FROM [{sqlTable}]");
                    }

                    txtDeleteStatus.Text = "Suppression effectuée.";
                }
            }
            catch (Exception ex)
            {
                txtDeleteStatus.Text = "Erreur: " + ex.Message;
            }
        }
    }
}
