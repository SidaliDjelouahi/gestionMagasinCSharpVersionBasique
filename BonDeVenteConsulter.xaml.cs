using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class BonDeVenteConsulter : Page
    {
        private readonly int _venteId;

        private class DetailItem
        {
            public int ProduitId { get; set; }
            public string Nom { get; set; } = string.Empty;
            public decimal PrixVente { get; set; }
            public int Qte { get; set; }
            public decimal Total { get; set; }
        }

        public BonDeVenteConsulter(int venteId)
        {
            InitializeComponent();
            _venteId = venteId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadDetails();
            }
            catch { }
        }

        private void LoadDetails()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var vente = db.Ventes.Find(_venteId);
                    if (vente != null)
                    {
                        txtNumVente.Text = vente.NumVente;
                        txtDateVente.Text = vente.Date.ToString();
                        try { txtVersement.Text = vente.Versement.ToString("0.00"); } catch { txtVersement.Text = "0.00"; }
                    }

                    var details = (from d in db.VenteDetails
                                   where d.VenteId == _venteId
                                   join p in db.Products on d.ProduitId equals p.Id
                                   select new DetailItem
                                   {
                                       ProduitId = p.Id,
                                       Nom = p.Nom,
                                       PrixVente = d.PrixVente,
                                       Qte = d.Qte,
                                       Total = d.PrixVente * d.Qte
                                   }).ToList();
                    dgVenteDetails.ItemsSource = details;
                    try
                    {
                        var total = details.Sum(x => x.Total);
                        txtTotalVente.Text = total.ToString("0.00");
                    }
                    catch { txtTotalVente.Text = "0.00"; }
                }
            }
            catch { }
        }

        private void btnUpdateVersement_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtVersement.Text))
            {
                MessageBox.Show("Entrez un montant de versement valide.");
                return;
            }

            if (!decimal.TryParse(txtVersement.Text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out var v))
            {
                var alt = txtVersement.Text?.Replace(',', '.');
                if (!decimal.TryParse(alt, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out v))
                {
                    MessageBox.Show("Montant de versement invalide.");
                    return;
                }
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var vente = db.Ventes.Find(_venteId);
                    if (vente != null)
                    {
                        vente.Versement = v;
                        db.SaveChanges();
                        MessageBox.Show("Versement mis à jour.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Erreur lors de la mise à jour : " + ex.Message);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
            else
                // fallback: navigate to Ventes page
                NavigationService?.Navigate(new Ventes());
        }

        private void Action_DeleteDetail_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.DataContext is DetailItem item))
                return;

            if (MessageBox.Show($"Supprimer la ligne '{item.Nom}' et restituer {item.Qte} au stock ?", "Confirmer", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var detail = db.VenteDetails.FirstOrDefault(d => d.VenteId == _venteId && d.ProduitId == item.ProduitId);
                    if (detail != null)
                    {
                        var prod = db.Products.Find(detail.ProduitId);
                        if (prod != null)
                        {
                            prod.Qte += detail.Qte;
                        }

                        db.VenteDetails.Remove(detail);
                        db.SaveChanges();
                    }
                }

                LoadDetails();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Erreur lors de la suppression de la ligne : " + ex.Message);
            }
        }

        private void btnDeleteVente_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Supprimer ce bon et restituer les quantités au stock ?", "Confirmer", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var details = db.VenteDetails.Where(d => d.VenteId == _venteId).ToList();
                    // restore quantities to products
                    foreach (var d in details)
                    {
                        var prod = db.Products.Find(d.ProduitId);
                        if (prod != null)
                        {
                            prod.Qte += d.Qte;
                        }
                    }

                    // remove details and the vente
                    db.VenteDetails.RemoveRange(details);
                    var vente = db.Ventes.Find(_venteId);
                    if (vente != null)
                        db.Ventes.Remove(vente);

                    db.SaveChanges();
                }

                MessageBox.Show("Bon supprimé et quantités remises en stock.");

                // navigate back to previous page or to Ventes
                if (this.NavigationService != null && this.NavigationService.CanGoBack)
                    this.NavigationService.GoBack();
                else
                    NavigationService?.Navigate(new Ventes());
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Erreur lors de la suppression : " + ex.Message);
            }
        }
    }
}
