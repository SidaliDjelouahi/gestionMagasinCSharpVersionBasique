using System.Windows;
using System.Windows.Controls;

namespace MonAppGestion
{
    public partial class Dashboard : Window
    {
        public Dashboard()
        {
            InitializeComponent();

            // Role-based button visibility
            ApplyRoleVisibility();

            // Afficher la page BonDeVente par défaut
            MainFrame.Navigate(new BonDeVente());
        }

        private void ApplyRoleVisibility()
        {
            var role = Session.CurrentUser?.Rank?.ToLowerInvariant() ?? string.Empty;

            // Default: show all
            btnUtilisateurs.Visibility = Visibility.Visible;
            btnProduits.Visibility = Visibility.Visible;
            btnClients.Visibility = Visibility.Visible;
            btnBonAchat.Visibility = Visibility.Visible;
            btnAchats.Visibility = Visibility.Visible;
            btnAnalyse.Visibility = Visibility.Visible;
            btnSauvegardeDB.Visibility = Visibility.Visible;
            btnFournisseurs.Visibility = Visibility.Visible;
            btnVentes.Visibility = Visibility.Visible;
            btnBonDeVente.Visibility = Visibility.Visible;
            btnAutre.Visibility = Visibility.Visible;

            if (role == "assistant")
            {
                // assistant: hide Sauvegarde DB, Analyse, Produits, Fournisseurs
                // also hide Utilisateurs
                btnUtilisateurs.Visibility = Visibility.Collapsed;
                btnSauvegardeDB.Visibility = Visibility.Collapsed;
                btnAnalyse.Visibility = Visibility.Collapsed;
                btnProduits.Visibility = Visibility.Collapsed;
                btnFournisseurs.Visibility = Visibility.Collapsed;
            }
            else if (role == "user")
            {
                // user: only show Ventes and Bon de vente
                btnUtilisateurs.Visibility = Visibility.Collapsed;
                btnProduits.Visibility = Visibility.Collapsed;
                btnClients.Visibility = Visibility.Collapsed;
                btnBonAchat.Visibility = Visibility.Collapsed;
                btnAchats.Visibility = Visibility.Collapsed;
                btnAnalyse.Visibility = Visibility.Collapsed;
                btnSauvegardeDB.Visibility = Visibility.Collapsed;
                btnFournisseurs.Visibility = Visibility.Collapsed;
                btnAutre.Visibility = Visibility.Collapsed;
                // keep btnVentes and btnBonDeVente visible
            }
            else
            {
                // developer/admin: show all (do nothing)
            }
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

        private void btnVentes_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Ventes());
        }

        private void btnBonDeVente_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BonDeVente());
        }

        private void btnClients_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Clients());
        }

        private void btnFournisseurs_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Fournisseurs());
        }

        private void btnBonAchat_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BonAchat());
        }

        private void btnAchats_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Achats());
        }

        private void btnAnalyse_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Analyse());
        }

        private void btnSauvegardeDB_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SauvegardeDB());
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