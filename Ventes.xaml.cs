using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

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

                        var items = ventes.Select(v => new
                        {
                            v.Id,
                            v.NumVente,
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
    }
}
