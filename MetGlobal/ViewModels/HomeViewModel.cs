using MetGlobal.Infrastructure;
using MetGlobal.Views;
using System.Windows;
using System.Windows.Input;

namespace MetGlobal.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public string AppVersion { get; } = "1.0.0";
        public string DeveloperName { get; } = "Тишин Максим Андреевич";
        public string INN { get; } = "3257065171";

        public ICommand ExitCommand { get; }
        public ICommand OpenHelpCommand { get; }

        public HomeViewModel()
        {
            ExitCommand = new RelayCommand(p => Application.Current.Shutdown());
            OpenHelpCommand = new RelayCommand(OpenHelp);
        }

        private void OpenHelp(object obj)
        {
            var helpWindow = new HelpWindow();
            helpWindow.Show(); 
        }
    }
}