using MetGlobal.ViewModels;
using System.Windows.Controls;

namespace MetGlobal.Views
{
    public partial class CustomersView : UserControl
    {
        public CustomersView()
        {
            InitializeComponent();
            DataContext = new CustomersViewModel();
        }
    }
}
