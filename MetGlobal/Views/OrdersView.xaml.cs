using MetGlobal.ViewModels;
using System.Windows.Controls;

namespace MetGlobal.Views
{
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent();
            DataContext = new OrdersViewModel();
        }
    }
}
