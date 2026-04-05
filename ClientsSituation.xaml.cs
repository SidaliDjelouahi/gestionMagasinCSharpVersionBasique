using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MonAppGestion.Models;
using Microsoft.EntityFrameworkCore;

namespace MonAppGestion
{
    public partial class ClientsSituation : Window
    {
        public ClientsSituation(int clientId, string clientName)
        {
            InitializeComponent();
            Title = $"Situation - {clientName}";
            LoadSituation(clientId);
        }

        private class VenteRow
        {
            public string NumVente { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public decimal Total { get; set; }
            public decimal Versement { get; set; }
        }

        private void LoadSituation(int clientId)
        {
            try
            {
                using var db = new AppDbContext();

                    var ventes = db.Ventes.Where(v => v.IdClient == clientId).AsNoTracking().ToList();
                    var venteIds = ventes.Select(v => v.Id).ToList();

                    var details = db.VenteDetails.Where(d => venteIds.Contains(d.VenteId)).AsEnumerable().ToList();

                    var rows = new List<VenteRow>();
                    foreach (var v in ventes)
                    {
                        var total = details.Where(d => d.VenteId == v.Id).Sum(d => d.PrixVente * d.Qte);
                        rows.Add(new VenteRow { NumVente = v.NumVente ?? string.Empty, Date = v.Date, Total = total, Versement = v.Versement });
                    }

                    var totalVentes = rows.Sum(r => r.Total);
                    var totalVersements = rows.Sum(r => r.Versement);
                    var reste = totalVentes - totalVersements;

                    txtTotalVentes.Text = totalVentes.ToString("0.00");
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
