using System.Windows;
using MonAppGestion.Models;

namespace MonAppGestion
{
    public partial class NewClientWindow : Window
    {
        public int CreatedClientId { get; private set; }

        public NewClientWindow()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var nom = txtNom.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(nom))
            {
                MessageBox.Show("Le nom est requis.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var client = new Client { Nom = nom, Adresse = txtAdresse.Text?.Trim() ?? string.Empty, Telephone = txtTelephone.Text?.Trim() ?? string.Empty };
            db.Clients.Add(client);
            db.SaveChanges();
            CreatedClientId = client.Id;
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
