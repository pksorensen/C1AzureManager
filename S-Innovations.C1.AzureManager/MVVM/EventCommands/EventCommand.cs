using System;
using System.Windows.Input;
namespace S_Innovations.C1.AzureManager.MVVM.EventCommands
{

        /// <summary>
        /// Extends <see cref="ICommand"/> interface with event related features like the Handled property.
        /// </summary>
        public interface IEventCommand : ICommand
        {
            /// <summary>
            /// Gets or sets value indicating wheather the Handled property of event arguments must be set to true.
            /// You can change this property at any time: during initialization or command execution.
            /// Note: Not all events has the Handled property!
            /// </summary>
            bool PreventBubbling { get; set; }
        }

        /// <summary>
        /// A command whose sole purpose is to relay its functionality to other objects by invoking delegates.
        /// The default return value for the CanExecute method is 'true'.
        /// This class allows you to accept event related parameters in the Execute and CanExecute callback methods.
        /// </summary>
        public class EventCommand<TParam> : IEventCommand
        {
            readonly Action<TParam> _execute;
            readonly Func<TParam, bool> _canExecute;

            /// <summary>
            /// Initializes a new instance of the RelayCommand class that can always execute.
            /// </summary>
            /// <param name="execute">The execution logic.</param>
            /// <exception cref="System.ArgumentNullException">If the execute argument is null.</exception>
            public EventCommand(Action<TParam> execute)
                : this(execute, parameter => true)
            {
            }

            /// <summary>
            /// Initializes a new instance of the RelayCommand class.
            /// </summary>
            /// <param name="execute">The execution logic.</param>
            /// <param name="canExecute">The execution status logic.</param>
            /// <exception cref="System.ArgumentNullException">If the execute or canExecute argument is null.</exception>
            public EventCommand(Action<TParam> execute, Func<TParam, bool> canExecute)
            {
                if (execute == null) throw new ArgumentNullException("execute");
                if (canExecute == null) throw new ArgumentNullException("canExecute");

                _execute = execute;
                _canExecute = canExecute;
            }

            /// <summary>
            /// Occurs when changes occur that affect whether the command should execute.
            /// </summary>
            public event EventHandler CanExecuteChanged;

            /// <summary>
            /// Defines the method that determines whether the command can execute in its current state.
            /// </summary>
            /// <param name="parameter">This parameter will be passed to <c>canExecute</c> delegate.</param>
            /// <returns>true if this command can be executed; otherwise, false.</returns>
            public bool CanExecute(object parameter)
            {
                return (parameter != null || typeof(TParam).IsByRef) && _canExecute((TParam)parameter);
            }

            /// <summary>
            /// Defines the method to be called when the command is invoked.
            /// </summary>
            /// <param name="parameter">This parameter will be passed to <c>execute</c> delegate.</param>
            public void Execute(object parameter)
            {
                if (CanExecute(parameter))
                    _execute((TParam)parameter);
            }

            /// <summary>
            /// Raises the CanExecuteChanged event.
            /// </summary>
            public void RaiseCanExecuteChanged()
            {
                if (CanExecuteChanged != null)
                    CanExecuteChanged(this, EventArgs.Empty);
            }

            #region Implementation of IEventCommand

            public bool PreventBubbling { get; set; }

            #endregion
        }
    
}
