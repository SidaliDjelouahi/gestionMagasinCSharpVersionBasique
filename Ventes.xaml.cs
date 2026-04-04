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
                    dgVentes.ItemsSource = db.Ventes.OrderByDescending(v => v.Date).ToList();
                }
            }
            catch { }
        }

        private void Action_Consulter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Vente v)
            {
                // Navigate to the consulter page showing the details of the sale
                NavigationService?.Navigate(new BonDeVenteConsulter(v.Id));
            }
        }
    }
}
