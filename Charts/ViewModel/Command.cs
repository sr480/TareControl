using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Charts.ViewModel
{
    public class Command : System.Windows.Input.ICommand
    {
        readonly Action<object> _executeAction;
        readonly Predicate<object> _canExecute;

        /// <summary>
        /// Инициализация команды
        /// </summary>
        /// <param name="executeAction">Выполняемое действие</param>
        /// <param name="canExecute">Функция, вычисляющая возможность вычисления</param>
        public Command(Action<object> executeAction, Predicate<object> canExecute)
        {
            if (executeAction == null) throw new ArgumentNullException("executeAction", "executeAction is null.");
            if (canExecute == null) throw new ArgumentNullException("canExecute", "canExecute is null.");

            _executeAction = executeAction;
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                throw new Exception("Команда недоступна");
            _executeAction(parameter);
        }

        /// <summary>
        /// Запуск извещения об изменении возможности исполнения
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, new EventArgs());
        }
    }
}
