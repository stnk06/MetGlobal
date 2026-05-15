using MetGlobal.ViewModels;
using System.Windows.Controls;

namespace MetGlobal.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = new ReportsViewModel();
            }
        }
    }
}