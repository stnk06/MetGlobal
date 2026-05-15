using MetGlobal.ViewModels;
using System.Windows.Controls;

namespace MetGlobal.Views
{
    /// <summary>
    /// Логика взаимодействия для CategoriesView.xaml
    /// </summary>
    public partial class CategoriesView : UserControl
    {
        public CategoriesView()
        {
            InitializeComponent();         
            DataContext = new CategoriesViewModel();
        }
    }
}
