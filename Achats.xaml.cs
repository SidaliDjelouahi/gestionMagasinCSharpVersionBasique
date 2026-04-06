using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MonAppGestion.Models;
using System.Globalization;

namespace MonAppGestion
{
    public partial class Achats : Page
    {
        public Achats()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var achats = db.Achats.OrderByDescending(v => v.Date).ToList();
                    var details = db.AchatDetails.ToList();

                    var totals = details.GroupBy(d => d.AchatId)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.PrixAchat * x.Qte));

                    var fournisseurs = db.Fournisseurs.ToList().ToDictionary(c => c.Id, c => c.Nom);

                    var items = achats.Select(v => new
                    {
                        v.Id,
                        v.NumAchat,
                        FournisseurName = (v.IdFournisseur.HasValue && fournisseurs.ContainsKey(v.IdFournisseur.Value)) ? fournisseurs[v.IdFournisseur.Value] : string.Empty,
                        v.Date,
                        Total = totals.ContainsKey(v.Id) ? totals[v.Id] : 0m
                    }).ToList();

                    dgAchats.ItemsSource = items;
                }
            }
            catch (System.Exception ex)
            {
                try { System.IO.File.AppendAllText("error.log", $"[Achats.Page_Loaded outer] {DateTime.Now}\n{ex}\n\n"); } catch { }
                MessageBox.Show("Erreur inattendue : " + ex.Message);
            }
        }

        private void Action_Consulter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn)
                {
                    var ctx = btn.DataContext;
                    if (ctx == null) return;

                    int id = 0;
                    var prop = ctx.GetType().GetProperty("Id");
                    if (prop != null)
                    {
                        var val = prop.GetValue(ctx);
                        if (val is int iv) id = iv;
                        else if (val is long lv) id = (int)lv;
                        else if (val != null) id = Convert.ToInt32(val);
                    }

                    if (id <= 0) return;

                    // Show details in a window using FlowDocument (read-only view)
                    using (var db = new AppDbContext())
                    {
                        var achat = db.Achats.Find(id);
                        if (achat == null) return;

                        var details = (from d in db.AchatDetails
                                       where d.AchatId == id
                                       join p in db.Products on d.ProduitId equals p.Id
                                       select new { p.Nom, d.Qte, d.PrixAchat }).ToList();

                        // Navigate to consulter page for this achat
                        NavigationService?.Navigate(new BonAchatConsulter(id));
                    }
                }
            }
            catch { }
        }

        private FlowDocument BuildAchatFlowDocument(Achat achat, System.Collections.IEnumerable details, AppDbContext db)
        {
            var fd = new FlowDocument();
            fd.PagePadding = new Thickness(12);
            fd.ColumnWidth = 300;

            var header = new Paragraph(new Bold(new Run("Bon d'achat"))) { FontSize = 16, TextAlignment = TextAlignment.Center };
            fd.Blocks.Add(header);

            var meta = new Paragraph();
            meta.Inlines.Add(new Run($"N°: {achat.NumAchat}") { FontWeight = FontWeights.Bold });
            meta.Inlines.Add(new LineBreak());
            meta.Inlines.Add(new Run($"Date: {achat.Date.ToString("g", CultureInfo.CurrentCulture)}"));
            meta.Inlines.Add(new LineBreak());
            try
            {
                if (achat.IdFournisseur.HasValue && achat.IdFournisseur.Value != 0)
                {
                    var f = db.Fournisseurs.Find(achat.IdFournisseur.Value);
                    if (f != null)
                        meta.Inlines.Add(new Run($"Fournisseur: {f.Nom}"));
                }
            }
            catch { }
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

            decimal total = 0m;
            foreach (dynamic d in details)
            {
                var r = new TableRow();
                r.Cells.Add(new TableCell(new Paragraph(new Run(d.Nom))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(d.Qte.ToString()))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(d.PrixAchat.ToString("0.00")))));
                var lineTotal = d.PrixAchat * d.Qte;
                r.Cells.Add(new TableCell(new Paragraph(new Run(lineTotal.ToString("0.00")))));
                rowGroup.Rows.Add(r);
                total += lineTotal;
            }

            table.RowGroups.Add(rowGroup);
            fd.Blocks.Add(table);

            var totals = new Paragraph();
            totals.TextAlignment = TextAlignment.Right;
            totals.Inlines.Add(new Run($"Total: {total:0.00}") { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new LineBreak());
            totals.Inlines.Add(new Run($"Versement: {achat.Versement:0.00}"));
            try
            {
                totals.Inlines.Add(new LineBreak());
                totals.Inlines.Add(new Run($"Reste: {(total - achat.Versement):0.00}") { FontWeight = FontWeights.Bold });
            }
            catch { }
            fd.Blocks.Add(totals);

            fd.Blocks.Add(new Paragraph(new Run("Merci.")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 12, 0, 0) });

            return fd;
        }

        private void Action_Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is Button btn)) return;

                var ctx = btn.DataContext;
                if (ctx == null) return;

                int id = 0;
                var prop = ctx.GetType().GetProperty("Id");
                if (prop != null)
                {
                    var val = prop.GetValue(ctx);
                    if (val is int iv) id = iv;
                    else if (val is long lv) id = (int)lv;
                    else if (val != null) id = Convert.ToInt32(val);
                }

                if (id <= 0) return;

                using (var db = new AppDbContext())
                {
                    var achat = db.Achats.Find(id);
                    if (achat == null) return;

                    var details = (from d in db.AchatDetails
                                   where d.AchatId == id
                                   join p in db.Products on d.ProduitId equals p.Id
                                   select new { p.Nom, d.Qte, d.PrixAchat }).ToList();

                    if (!details.Any())
                    {
                        MessageBox.Show("Aucune ligne à imprimer pour cet achat.", "Imprimer", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var fd = BuildAchatFlowDocument(achat, details, db);

                    var pd = new PrintDialog();
                    if (pd.ShowDialog() == true)
                    {
                        IDocumentPaginatorSource idp = fd;
                        try { pd.PrintDocument(idp.DocumentPaginator, "BonAchat"); }
                        catch (System.Exception ex) { MessageBox.Show("Erreur impression: " + ex.Message); }
                    }
                }
            }
            catch { }
        }
    }
}
