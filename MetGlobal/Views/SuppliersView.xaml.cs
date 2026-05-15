using MetGlobal.ViewModels;
using System.Windows.Controls;

namespace MetGlobal.Views
{
    public partial class SuppliersView : UserControl
    {
        public SuppliersView()
        {
            InitializeComponent();
            DataContext = new SuppliersViewModel();
        }
    }
}
