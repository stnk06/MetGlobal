using MetGlobal.Models;
using MetGlobal.ViewModels;
using System.Windows;

namespace MetGlobal.Views
{
    public partial class EditOrderWindow : Window
    {
        public EditOrderWindow(Order order)
        {
            InitializeComponent();
            DataContext = new EditOrderViewModel(order);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as EditOrderViewModel;
            if (vm.CurrentOrder.CustomerID == 0)
            {
                MessageBox.Show("Необходимо выбрать клиента.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (vm.OrderDetails.Count == 0)
            {
                MessageBox.Show("Заказ не может быть пустым.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.DialogResult = true;
        }
    }
}
