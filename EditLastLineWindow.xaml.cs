using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace MonAppGestion
{
    public partial class EditLastLineWindow : Window
    {
        public string? ProductName { get; private set; }
        public int Qty { get; private set; }
        public decimal Price { get; private set; }

        public EditLastLineWindow(string name, int qty, decimal price)
        {
            InitializeComponent();
            txtName.Text = name ?? string.Empty;
            txtQty.Text = qty.ToString();
            txtPrice.Text = price.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtQty.Focus();
                Keyboard.Focus(txtQty);
                txtQty.SelectAll();
            }
            catch { }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            // Validate qty and price
            if (!int.TryParse(txtQty.Text, out var q))
            {
                MessageBox.Show("Entrez une quantité valide.");
                return;
            }
            if (!decimal.TryParse(txtPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var p))
            {
                // try current culture as fallback
                if (!decimal.TryParse(txtPrice.Text, out p))
                {
                    MessageBox.Show("Entrez un prix valide.");
                    return;
                }
            }

            ProductName = txtName.Text;
            Qty = q;
            Price = p;
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
