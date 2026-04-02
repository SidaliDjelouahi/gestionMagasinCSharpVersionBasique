using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class Utilisateurs : Page
    {
        public Utilisateurs()
        {
            InitializeComponent();
            ChargerUtilisateurs();
        }

        private void ChargerUtilisateurs()
        {
            using (var db = new AppDbContext())
            {
                var liste = db.Users.ToList();
                dgUsers.ItemsSource = liste;
            }
        }

        private void btnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewUser.Text) || string.IsNullOrWhiteSpace(txtNewPass.Text))
            {
                MessageBox.Show("Veuillez remplir tous les champs.");
                return;
            }

            using (var db = new AppDbContext())
            {
                var nvxUser = new User { Username = txtNewUser.Text, Password = txtNewPass.Text };
                db.Users.Add(nvxUser);
                db.SaveChanges();
            }

            txtNewUser.Clear();
            txtNewPass.Clear();
            ChargerUtilisateurs();
        }

        private void btnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = dgUsers.SelectedItem as User;

            if (selectedUser == null)
            {
                MessageBox.Show("Sélectionnez d'abord un utilisateur dans le tableau.");
                return;
            }

            if (MessageBox.Show($"Supprimer l'utilisateur {selectedUser.Username} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    db.Users.Remove(selectedUser);
                    db.SaveChanges();
                }
                ChargerUtilisateurs();
            }
        }
    }
}