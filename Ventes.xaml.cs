using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MonAppGestion.Models;
using System.Globalization;

namespace MonAppGestion
{
    public partial class Ventes : Page
    {
        public Ventes()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                        // Load ventes and details, compute totals client-side to avoid SQLite decimal Sum issues
                        var ventes = db.Ventes.OrderByDescending(v => v.Date).ToList();
                        var details = db.VenteDetails.ToList();

                        var totals = details.GroupBy(d => d.VenteId)
                            .ToDictionary(g => g.Key, g => g.Sum(x => x.PrixVente * x.Qte));

                        var clients = db.Clients.ToList().ToDictionary(c => c.Id, c => c.Nom);

                        var items = ventes.Select(v => new
                        {
                            v.Id,
                            v.NumVente,
                            ClientName = (v.IdClient.HasValue && clients.ContainsKey(v.IdClient.Value)) ? clients[v.IdClient.Value] : string.Empty,
                            v.Date,
                            Total = totals.ContainsKey(v.Id) ? totals[v.Id] : 0m
                        }).ToList();

                        dgVentes.ItemsSource = items;
                }
            }
            catch (System.Exception ex)
            {
                try { System.IO.File.AppendAllText("error.log", $"[Ventes.Page_Loaded outer] {DateTime.Now}\n{ex}\n\n"); } catch { }
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
                    if (ctx is Vente v)
                    {
                        id = v.Id;
                    }
                    else
                    {
                        var prop = ctx.GetType().GetProperty("Id");
                        if (prop != null)
                        {
                            var val = prop.GetValue(ctx);
                            if (val is int iv) id = iv;
                            else if (val is long lv) id = (int)lv;
                            else if (val != null) id = Convert.ToInt32(val);
                        }
                    }

                    if (id > 0)
                        NavigationService?.Navigate(new BonDeVenteConsulter(id));
                }
            }
            catch { }
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
                    var vente = db.Ventes.Find(id);
                    if (vente == null) return;

                    var details = (from d in db.VenteDetails
                                   where d.VenteId == id
                                   join p in db.Products on d.ProduitId equals p.Id
                                   select new { p.Nom, d.Qte, d.PrixVente }).ToList();

                    if (!details.Any())
                    {
                        MessageBox.Show("Aucune ligne à imprimer pour cette vente.", "Imprimer", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Build FlowDocument similar to BonDeVente
                    var fd = new FlowDocument();
                    fd.PagePadding = new Thickness(12);
                    fd.ColumnWidth = 300;

                    var header = new Paragraph(new Bold(new Run("Bon de vente"))) { FontSize = 16, TextAlignment = TextAlignment.Center };
                    fd.Blocks.Add(header);

                    var meta = new Paragraph();
                    meta.Inlines.Add(new Run($"N°: {vente.NumVente}") { FontWeight = FontWeights.Bold });
                    meta.Inlines.Add(new LineBreak());
                    meta.Inlines.Add(new Run($"Date: {vente.Date.ToString("g", CultureInfo.CurrentCulture)}"));
                    meta.Inlines.Add(new LineBreak());
                    try
                    {
                        if (vente.IdClient.HasValue && vente.IdClient.Value != 0)
                        {
                            var cl = db.Clients.Find(vente.IdClient.Value);
                            if (cl != null)
                                meta.Inlines.Add(new Run($"Client: {cl.Nom}"));
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
                    foreach (var d in details)
                    {
                        var r = new TableRow();
                        r.Cells.Add(new TableCell(new Paragraph(new Run(d.Nom))));
                        r.Cells.Add(new TableCell(new Paragraph(new Run(d.Qte.ToString()))));
                        r.Cells.Add(new TableCell(new Paragraph(new Run(d.PrixVente.ToString("0.00")))));
                        var lineTotal = d.PrixVente * d.Qte;
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
                    totals.Inlines.Add(new Run($"Versement: {vente.Versement:0.00}"));
                    try
                    {
                        totals.Inlines.Add(new LineBreak());
                        totals.Inlines.Add(new Run($"Reste: {(total - vente.Versement):0.00}") { FontWeight = FontWeights.Bold });
                    }
                    catch { }
                    fd.Blocks.Add(totals);

                    fd.Blocks.Add(new Paragraph(new Run("Merci pour votre achat")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 12, 0, 0) });

                    var pd = new PrintDialog();
                    if (pd.ShowDialog() == true)
                    {
                        IDocumentPaginatorSource idp = fd;
                        try { pd.PrintDocument(idp.DocumentPaginator, "BonDeVente"); }
                        catch (System.Exception ex) { MessageBox.Show("Erreur impression: " + ex.Message); }
                    }
                }
            }
            catch { }
        }
    }
}
