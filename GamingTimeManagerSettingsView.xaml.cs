using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace GamingTimeManager
{
    public partial class GamingTimeManagerSettingsView : UserControl
    {
        public GamingTimeManagerSettingsView()
        {
            InitializeComponent();
        }

        private void ValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

    }
}