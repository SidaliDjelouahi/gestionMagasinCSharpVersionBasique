using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class BonAchatConsulter : Page
    {
        private readonly int _achatId;

        private class DetailItem
        {
            public int ProduitId { get; set; }
            public string Nom { get; set; } = string.Empty;
            public decimal PrixAchat { get; set; }
            public int Qte { get; set; }
            public decimal Total { get; set; }
        }

        public BonAchatConsulter(int achatId)
        {
            InitializeComponent();
            _achatId = achatId;
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
                    var achat = db.Achats.Include("Fournisseur").FirstOrDefault(v => v.Id == _achatId);
                    if (achat != null)
                    {
                        txtNumAchat.Text = achat.NumAchat;
                        txtDateAchat.Text = achat.Date.ToString();
                        try { txtVersement.Text = achat.Versement.ToString("0.00"); } catch { txtVersement.Text = "0.00"; }

                        try
                        {
                            if (achat.Fournisseur != null)
                                txtFournisseurName.Text = achat.Fournisseur.Nom;
                            else if (achat.IdFournisseur.HasValue && achat.IdFournisseur.Value != 0)
                            {
                                var f = db.Fournisseurs.Find(achat.IdFournisseur.Value);
                                txtFournisseurName.Text = f?.Nom ?? string.Empty;
                            }
                            else
                            {
                                txtFournisseurName.Text = string.Empty;
                            }
                        }
                        catch { txtFournisseurName.Text = string.Empty; }
                    }

                    var details = (from d in db.AchatDetails
                                   where d.AchatId == _achatId
                                   join p in db.Products on d.ProduitId equals p.Id
                                   select new DetailItem
                                   {
                                       ProduitId = p.Id,
                                       Nom = p.Nom,
                                       PrixAchat = d.PrixAchat,
                                       Qte = d.Qte,
                                       Total = d.PrixAchat * d.Qte
                                   }).ToList();

                    dgAchatDetails.ItemsSource = details;
                    try
                    {
                        var total = details.Sum(x => x.Total);
                        txtTotalAchat.Text = total.ToString("0.00");
                    }
                    catch { txtTotalAchat.Text = "0.00"; }
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
                    var achat = db.Achats.Find(_achatId);
                    if (achat != null)
                    {
                        achat.Versement = v;
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
                NavigationService?.Navigate(new Achats());
        }

        private void Action_DeleteDetail_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.DataContext is DetailItem item))
                return;

            if (MessageBox.Show($"Supprimer la ligne '{item.Nom}' et retirer {item.Qte} du stock ?", "Confirmer", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var detail = db.AchatDetails.FirstOrDefault(d => d.AchatId == _achatId && d.ProduitId == item.ProduitId);
                    if (detail != null)
                    {
                        var prod = db.Products.Find(detail.ProduitId);
                        if (prod != null)
                        {
                            prod.Qte = Math.Max(0, prod.Qte - detail.Qte);
                        }

                        db.AchatDetails.Remove(detail);
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

        private void btnDeleteAchat_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Supprimer ce bon et retirer les quantités du stock ?", "Confirmer", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var details = db.AchatDetails.Where(d => d.AchatId == _achatId).ToList();
                    // remove quantities from products
                    foreach (var d in details)
                    {
                        var prod = db.Products.Find(d.ProduitId);
                        if (prod != null)
                        {
                            prod.Qte = Math.Max(0, prod.Qte - d.Qte);
                        }
                    }

                    db.AchatDetails.RemoveRange(details);
                    var achat = db.Achats.Find(_achatId);
                    if (achat != null)
                        db.Achats.Remove(achat);

                    db.SaveChanges();
                }

                MessageBox.Show("Bon supprimé et quantités retirées du stock.");

                if (this.NavigationService != null && this.NavigationService.CanGoBack)
                    this.NavigationService.GoBack();
                else
                    NavigationService?.Navigate(new Achats());
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Erreur lors de la suppression : " + ex.Message);
            }
        }
    }
}
