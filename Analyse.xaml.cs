using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class Analyse : Page
    {
        public Analyse()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStats();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadStats();
        }

        private void LoadStats()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Fetch needed data into memory to avoid provider-side translation issues with SQLite
                    var productsList = db.Products.Select(p => new { p.Id, p.Qte, p.PrixAchat }).ToList();
                    var productsMap = productsList.ToDictionary(p => p.Id, p => p.PrixAchat);

                    // 1. Solde inventaire = sum(products.Qte * products.PrixAchat)
                    var soldeInventaire = productsList.Sum(p => p.Qte * p.PrixAchat);
                    txtSoldeInventaire.Text = soldeInventaire.ToString("0.00");

                    // 2. Total vente du jour = sum over venteDetails for ventes with Date == today of PrixVente * Qte
                    var today = DateTime.Today;
                    var ventesToday = db.Ventes.Where(v => v.Date.Date == today).Select(v => new { v.Id, v.Versement, v.NumVente, v.Date }).ToList();
                    var ventesTodayIds = ventesToday.Select(v => v.Id).ToList();
                    var venteDetailsToday = db.VenteDetails.Where(d => ventesTodayIds.Contains(d.VenteId)).Select(d => new { d.PrixVente, d.Qte }).ToList();
                    var totalVentesToday = venteDetailsToday.Sum(d => d.PrixVente * d.Qte);
                    txtTotalVentesAujourdHui.Text = totalVentesToday.ToString("0.00");

                    // 3. Solde caisse = sum of Versement for ventes today
                    var soldeCaisse = ventesToday.Sum(v => v.Versement);
                    txtSoldeCaisse.Text = soldeCaisse.ToString("0.00");

                    // 4. Total benefice for each bon if versement is complete (versement >= total)
                    var allDetails = db.VenteDetails.Select(d => new { d.VenteId, d.ProduitId, d.PrixVente, d.Qte }).ToList();

                    var venteTotals = allDetails.GroupBy(d => d.VenteId)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.PrixVente * x.Qte));

                    // Pull ventes into memory first, then filter using venteTotals dictionary
                    var ventesAll = db.Ventes.Select(v => new { v.Id, v.NumVente, v.Date, v.Versement }).ToList();
                    var ventesCompletes = ventesAll.Where(v => venteTotals.ContainsKey(v.Id) && v.Versement >= venteTotals[v.Id]).ToList();

                    var beneficeList = ventesCompletes.Select(v =>
                    {
                        var details = allDetails.Where(d => d.VenteId == v.Id);
                        var total = details.Sum(d => d.PrixVente * d.Qte);
                        var benef = details.Sum(d => (d.PrixVente - (productsMap.ContainsKey(d.ProduitId) ? productsMap[d.ProduitId] : 0m)) * d.Qte);
                        return new { Id = v.Id, NumVente = v.NumVente, Date = v.Date, Total = total, Benefice = benef };
                    }).ToList();

                    dgBenefices.ItemsSource = beneficeList;
                    var totalBenef = beneficeList.Sum(x => x.Benefice);
                    txtTotalBenefice.Text = totalBenef.ToString("0.00");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du calcul des statistiques: " + ex.Message);
            }
        }

        private void ExplainInventaire_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Solde inventaire = somme pour chaque produit de (quantité en stock * prix d'achat). Cela représente la valeur totale du stock au prix d'achat.", "Explication Solde Inventaire", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExplainVentesToday_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Total ventes aujourd'hui = somme des (prix de vente * quantité) pour toutes les lignes de vente dont la date du bon est aujourd'hui.", "Explication Ventes Aujourd'hui", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExplainCaisse_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Solde caisse = somme des montants de versement enregistrés pour les bons de vente datés d'aujourd'hui. Cela représente l'encaissement journalier.", "Explication Solde Caisse", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExplainBenefice_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pour chaque bon dont le versement est complet (versement >= total du bon), le bénéfice est calculé comme somme pour chaque ligne: (prix de vente - prix d'achat) * quantité. Le total affiché est la somme des bénéfices de ces bons.", "Explication Bénéfice", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
