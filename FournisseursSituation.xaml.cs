using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class FournisseursSituation : Window
    {
        public FournisseursSituation(int fournisseurId, string fournisseurName)
        {
            InitializeComponent();
            Title = $"Situation - {fournisseurName}";
            LoadSituation(fournisseurId);
        }

        private class AchatRow
        {
            public string NumAchat { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public decimal Total { get; set; }
            public decimal Versement { get; set; }
        }

        private void LoadSituation(int fournisseurId)
        {
            try
            {
                using var db = new AppDbContext();
                var achats = db.Achats.Where(a => a.IdFournisseur == fournisseurId).AsNoTracking().ToList();
                var achatIds = achats.Select(a => a.Id).ToList();

                var details = db.AchatDetails.Where(d => achatIds.Contains(d.AchatId)).AsEnumerable().ToList();

                var rows = new List<AchatRow>();
                foreach (var a in achats)
                {
                    var total = details.Where(d => d.AchatId == a.Id).Sum(d => d.PrixAchat * d.Qte);
                    rows.Add(new AchatRow { NumAchat = a.NumAchat ?? string.Empty, Date = a.Date, Total = total, Versement = a.Versement });
                }

                var totalAchats = rows.Sum(r => r.Total);
                var totalVersements = rows.Sum(r => r.Versement);
                var reste = totalAchats - totalVersements;

                txtTotalAchats.Text = totalAchats.ToString("0.00");
                txtTotalVersements.Text = totalVersements.ToString("0.00");
                txtReste.Text = reste.ToString("0.00");

                dgSituation.ItemsSource = rows.OrderByDescending(r => r.Date).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la situation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
