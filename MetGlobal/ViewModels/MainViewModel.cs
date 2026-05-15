using MetGlobal.Infrastructure;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MetGlobal.ViewModels
{
    public class MenuItem : ViewModelBase
    {
        public string Icon { get; set; }
        public string Label { get; set; }
        public object ViewModel { get; set; }
        public ICommand Command { get; set; }
    }

    public class MainViewModel : ViewModelBase
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MenuItem> MenuItems { get; set; }

        private MenuItem _selectedMenuItem;
        public MenuItem SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (_selectedMenuItem != value)
                {
                    _selectedMenuItem = value;

                    if (value?.Command != null)
                    {
                        value.Command.Execute(null);
                        SelectedMenuItem = null;
                    }
                    else if (value?.ViewModel != null)
                    {
                        CurrentView = value.ViewModel;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel()
        {
            var homeVM = new HomeViewModel();
            var ordersVM = new OrdersViewModel();
            var productsVM = new ProductsViewModel();
            var customersVM = new CustomersViewModel();
            var reportsVM = new ReportsViewModel();
            var categoriesVM = new CategoriesViewModel();
            var suppliersVM = new SuppliersViewModel();
            MenuItems = new ObservableCollection<MenuItem>
            {
                new MenuItem { Icon = "\uE80F", Label = "Главная", ViewModel = homeVM },
                new MenuItem { Icon = "\uE8A5", Label = "Заказы", ViewModel = ordersVM },
                new MenuItem { Icon = "\uE71D", Label = "Продукция", ViewModel = productsVM },
                new MenuItem { Icon = "\uE716", Label = "Клиенты", ViewModel = customersVM },
                new MenuItem { Icon = "\uE9F9", Label = "Отчеты", ViewModel = reportsVM },
                new MenuItem { Icon = "\uE719", Label = "Категории", ViewModel = categoriesVM },
                new MenuItem { Icon = "\uE77B", Label = "Поставщики", ViewModel = suppliersVM },
                new MenuItem { Icon = "\uE7E8", Label = "Выход", Command = new RelayCommand(p => Application.Current.Shutdown()) }
            };
            SelectedMenuItem = MenuItems[0];
        }
    }
}