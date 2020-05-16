#region

using System;
using System.Windows.Input;

#endregion

namespace Drexel.VidUp.UI
{
    public class GenericCommand : ICommand
    {
        private Action<object> execute;
        private bool canExecute = true;

        public event EventHandler CanExecuteChanged;

        public GenericCommand(Action<object> execute)
        {
            this.execute = execute;
        }
        public void SetCanExecute(bool canExecute)
        {
            if (this.canExecute != canExecute)
            {
                this.canExecute = canExecute;
                this.raiseCanExecuteChanged();
            }
        }
        public bool CanExecute(object parameter)
        {
            return this.canExecute;
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }

        private void raiseCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static bool defaultCanExecute(object parameter)
        {
            return true;
        }
    }
}
