using System;
using System.Windows.Input;

namespace MetGlobal.Infrastructure
{
    /// <summary>
    /// Универсальная реализация интерфейса ICommand для MVVM.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// Конструктор команды.
        /// </summary>
        /// <param name="execute">Действие, которое нужно выполнить.</param>
        /// <param name="canExecute">Условие, при котором действие можно выполнить.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Событие, которое оповещает об изменении возможности выполнения команды.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Проверяет, может ли команда выполняться в данный момент.
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Выполняет логику команды.
        /// </summary>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
