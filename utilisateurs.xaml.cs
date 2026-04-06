using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonAppGestion.Models;

// Support editing selected user via the top input fields

namespace MonAppGestion
{
    public partial class Utilisateurs : Page
    {
        private int? _editingUserId = null;

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
            if (string.IsNullOrWhiteSpace(txtNewUser.Text) || string.IsNullOrWhiteSpace(txtNewPass.Text) || cbRank.SelectedItem==null)
            {
                MessageBox.Show("Veuillez remplir tous les champs.");
                return;
            }

            using (var db = new AppDbContext())
            {
                    if (_editingUserId.HasValue)
                {
                    // Update existing user
                    var existing = db.Users.FirstOrDefault(u => u.Id == _editingUserId.Value);
                    if (existing != null)
                    {
                        existing.Username = txtNewUser.Text;
                        existing.Password = txtNewPass.Text;
                            existing.Rank = ((ComboBoxItem)cbRank.SelectedItem).Content.ToString();
                        db.SaveChanges();
                    }
                    _editingUserId = null;
                    btnAjouter.Content = "Ajouter";
                }
                else
                {
                    var nvxUser = new User { Username = txtNewUser.Text, Password = txtNewPass.Text, Rank = ((ComboBoxItem)cbRank.SelectedItem).Content.ToString() };
                    db.Users.Add(nvxUser);
                    db.SaveChanges();
                }
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

        private void btnModifier_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = dgUsers.SelectedItem as User;

            if (selectedUser == null)
            {
                MessageBox.Show("Sélectionnez d'abord un utilisateur dans le tableau.");
                return;
            }

            // Remplir les champs du haut pour modification
            txtNewUser.Text = selectedUser.Username;
            txtNewPass.Text = selectedUser.Password;
            // Select rank in combobox
            foreach (ComboBoxItem it in cbRank.Items)
            {
                if (string.Equals(it.Content.ToString(), selectedUser.Rank, System.StringComparison.OrdinalIgnoreCase))
                {
                    cbRank.SelectedItem = it;
                    break;
                }
            }
            _editingUserId = selectedUser.Id;
            btnAjouter.Content = "Enregistrer";
        }
    }
}