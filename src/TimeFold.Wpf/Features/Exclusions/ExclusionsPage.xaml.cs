using System.Windows.Controls;
using System.Windows.Input;

namespace TimeFold.Wpf.Features.Exclusions
{
    public partial class ExclusionsPage : UserControl
    {
        public ExclusionsPage()
        {
            InitializeComponent();
        }

        private void NewFolder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ExclusionsViewModel vm)
            {
                vm.AddCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
