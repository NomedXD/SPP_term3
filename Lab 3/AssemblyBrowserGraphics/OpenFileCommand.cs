using System;
using System.Windows.Input;

namespace AssemblyBrowserGraphics
{
    public class OpenFileCommand : ICommand
    {
        // Изменение источника команды
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        // Наша единственная команда на форме - открыть файл
        public OpenFileCommand(Action execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        private Action _execute;
        private Func<object, bool> _canExecute;

        // Метод определяет, может ли команда быть выполнена(реализуется от интерфейса)
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || CanExecute(parameter);
        }
        // Выполняет непосредственно действие команды(реализуется от интерфейса)
        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
