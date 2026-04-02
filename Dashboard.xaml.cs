using System.Windows;
using System.Windows.Controls;

namespace MonAppGestion
{
    public partial class Dashboard : Window
    {
        public Dashboard()
        {
            InitializeComponent();

            // Afficher la page BonDeVente par défaut
            MainFrame.Navigate(new BonDeVente());
        }

        private void btnUtilisateurs_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Utilisateurs());
        }

        private void btnProduits_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Produits());
        }

        private void btnAutre_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder : remplacer par la navigation vers une autre Page
            MainFrame.Content = new TextBlock { Text = "Page en construction", FontSize = 20, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        }

        private void btnBonDeVente_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BonDeVente());
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Ferme le Dashboard et retourne à la fenêtre de connexion
            var login = new MainWindow();
            login.Show();
            this.Close();
        }
    }
}