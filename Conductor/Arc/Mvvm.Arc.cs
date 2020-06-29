// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text
#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Mvvm
{
    public enum MessageId
    { // ViewService: MessageId
        Exit, // exit application with confirmation
        ExitWithoutConfirmation, // exit application without confirmation
        SwitchCulture, // switch culture
        Information, // information dialog
        Settings, // settings dialog
        Help, // help
        DisplayScaling, // Update display scaling.
        ActivateWindow, // Brings the window into the foreground and activates the window.
        ActivateWindowForce, // Brings the window into the foreground forcibly, and activates the window.
    }

    /// <summary>
    /// Base class for all messages broadcasted by the Messenger.
    /// You can create your own message types by extending this class.
    /// </summary>
    public class MessageBase
    {
        public MessageBase()
        {
        }

        public MessageBase(object sender)
        {
            this.Sender = sender;
        }

        public MessageBase(object sender, object target)
            : this(sender)
        {
            this.Target = target;
        }

        public object? Sender { get; protected set; } // the message's sender.

        public object? Target { get; protected set; } // the message's intended target.
    }

    public class GenericMessage<T> : MessageBase
    {
        public GenericMessage(T content)
        {
            this.Content = content;
        }

        public GenericMessage(object sender, T content)
            : base(sender)
        {
            this.Content = content;
        }

        public GenericMessage(object sender, object target, T content)
            : base(sender, target)
        {
            this.Content = content;
        }

        public T Content { get; protected set; } // the message's content.
    }

    public class NotificationMessage : MessageBase
    {
        public NotificationMessage(string notification)
        {
            this.Notification = notification;
        }

        public NotificationMessage(object sender, string notification)
            : base(sender)
        {
            this.Notification = notification;
        }

        public NotificationMessage(object sender, object target, string notification)
            : base(sender, target)
        {
            this.Notification = notification;
        }

        public string Notification { get; private set; }
    }

    /// <summary>
    /// Represents each node of nested properties expression and takes care of
    /// subscribing/unsubscribing INotifyPropertyChanged.PropertyChanged listeners on it.
    /// </summary>
    internal class PropertyObserverNode
    {
        private readonly Action action;
        private INotifyPropertyChanged? inpcObject;

        public PropertyInfo PropertyInfo { get; }

        public PropertyObserverNode? Next { get; set; }

        public PropertyObserverNode(PropertyInfo propertyInfo, Action action)
        {
            this.PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
            this.action = () =>
            {
                action?.Invoke();
                if (this.Next == null)
                {
                    return;
                }

                this.Next.UnsubscribeListener();
                this.GenerateNextNode();
            };
        }

        public void SubscribeListenerFor(INotifyPropertyChanged inpcObject)
        {
            this.inpcObject = inpcObject;
            this.inpcObject.PropertyChanged += this.OnPropertyChanged;

            if (this.Next != null)
            {
                this.GenerateNextNode();
            }
        }

        private void GenerateNextNode()
        {
            var nextProperty = this.PropertyInfo.GetValue(this.inpcObject);
            if (nextProperty == null)
            {
                return;
            }

            if (!(nextProperty is INotifyPropertyChanged nextInpcObject))
            {
                throw new InvalidOperationException("Trying to subscribe PropertyChanged listener in object that " + $"owns '{this.Next?.PropertyInfo.Name}' property, but the object does not implements INotifyPropertyChanged.");
            }

            this.Next?.SubscribeListenerFor(nextInpcObject);
        }

        private void UnsubscribeListener()
        {
            if (this.inpcObject != null)
            {
                this.inpcObject.PropertyChanged -= this.OnPropertyChanged;
            }

            this.Next?.UnsubscribeListener();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Invoke action when e.PropertyName == null in order to satisfy:
            //  - DelegateCommandFixture.GenericDelegateCommandObservingPropertyShouldRaiseOnEmptyPropertyName
            //  - DelegateCommandFixture.NonGenericDelegateCommandObservingPropertyShouldRaiseOnEmptyPropertyName
            if (e?.PropertyName == this.PropertyInfo.Name || e?.PropertyName == null)
            {
                this.action?.Invoke();
            }
        }
    }

    /// <summary>
    /// Provide a way to observe property changes of INotifyPropertyChanged objects and invokes a
    /// custom action when the PropertyChanged event is fired.
    /// </summary>
    internal class PropertyObserver
    {
        private readonly Action action;

        private PropertyObserver(Expression propertyExpression, Action action)
        {
            this.action = action;
            this.SubscribeListeners(propertyExpression);
        }

        private void SubscribeListeners(Expression propertyExpression)
        {
            var propNameStack = new Stack<PropertyInfo>();
            while (propertyExpression is MemberExpression temp)
            { // Gets the root of the property chain.
                propertyExpression = temp.Expression;
                propNameStack.Push((PropertyInfo)temp.Member); // Records the member info as property info
            }

            if (!(propertyExpression is ConstantExpression constantExpression))
            {
                throw new NotSupportedException("Operation not supported for the given expression type. " + "Only MemberExpression and ConstantExpression are currently supported.");
            }

            var propObserverNodeRoot = new PropertyObserverNode(propNameStack.Pop(), this.action);
            PropertyObserverNode previousNode = propObserverNodeRoot;
            foreach (var propName in propNameStack)
            { // Create a node chain that corresponds to the property chain.
                var currentNode = new PropertyObserverNode(propName, this.action);
                previousNode.Next = currentNode;
                previousNode = currentNode;
            }

            object propOwnerObject = constantExpression.Value;

            if (!(propOwnerObject is INotifyPropertyChanged inpcObject))
            {
                throw new InvalidOperationException("Trying to subscribe PropertyChanged listener in object that " + $"owns '{propObserverNodeRoot.PropertyInfo.Name}' property, but the object does not implements INotifyPropertyChanged.");
            }

            propObserverNodeRoot.SubscribeListenerFor(inpcObject);
        }

        /// <summary>
        /// Observes a property that implements INotifyPropertyChanged, and automatically calls a custom action on
        /// property changed notifications. The given expression must be in this form: "() => Prop.NestedProp.PropToObserve".
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="propertyExpression">Expression representing property to be observed. Ex.: "() => Prop.NestedProp.PropToObserve".</param>
        /// <param name="action">Action to be invoked when PropertyChanged event occours.</param>
        /// <returns>PropertyObserver.</returns>
        internal static PropertyObserver Observes<T>(Expression<Func<T>> propertyExpression, Action action)
        {
            return new PropertyObserver(propertyExpression.Body, action);
        }
    }

    /// <summary>
    /// Interface that defines if the object instance is active
    /// and notifies when the activity changes.
    /// </summary>
    public interface IActiveAware
    {
        /// <summary>
        /// Gets or sets a value indicating whether the object is active.
        /// </summary>
        /// <value><see langword="true" /> if the object is active; otherwise <see langword="false" />.</value>
        bool IsActive { get; set; }

        /// <summary>
        /// Notifies that the value for <see cref="IsActive"/> property has changed.
        /// </summary>
        event EventHandler IsActiveChanged;
    }

    /// <summary>
    /// An <see cref="ICommand"/> whose delegates can be attached for <see cref="Execute"/> and <see cref="CanExecute"/>.
    /// </summary>
    public abstract class DelegateCommandBase : ICommand, IActiveAware
    {
        private readonly HashSet<string> observedPropertiesExpressions = new HashSet<string>();
        private bool isActive;
        private SynchronizationContext? synchronizationContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommandBase"/> class.
        /// </summary>
        protected DelegateCommandBase()
        {
            this.synchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public virtual event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Raises <see cref="ICommand.CanExecuteChanged"/> so every
        /// command invoker can requery <see cref="ICommand.CanExecute"/>.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            var handler = this.CanExecuteChanged;
            if (handler != null)
            {
                if (this.synchronizationContext != null && this.synchronizationContext != SynchronizationContext.Current)
                {
                    this.synchronizationContext.Post((o) => handler.Invoke(this, EventArgs.Empty), null);
                }
                else
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Raises <see cref="CanExecuteChanged"/> so every command invoker
        /// can requery to check if the command can execute.
        /// </summary>
        /// <remarks>Note that this will trigger the execution of <see cref="CanExecuteChanged"/> once for each invoker.</remarks>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = ".")]
        public void RaiseCanExecuteChanged()
        {
            this.OnCanExecuteChanged();
        }

        void ICommand.Execute(object parameter)
        {
            this.Execute(parameter);
        }

        bool ICommand.CanExecute(object parameter)
        {
            return this.CanExecute(parameter);
        }

        /// <summary>
        /// Handle the internal invocation of <see cref="ICommand.Execute(object)"/>.
        /// </summary>
        /// <param name="parameter">Command Parameter.</param>
        protected abstract void Execute(object parameter);

        /// <summary>
        /// Handle the internal invocation of <see cref="ICommand.CanExecute(object)"/>.
        /// </summary>
        /// <param name="parameter">parameter.</param>
        /// <returns><see langword="true"/> if the Command Can Execute, otherwise <see langword="false" />.</returns>
        protected abstract bool CanExecute(object parameter);

        /// <summary>
        /// Observes a property that implements INotifyPropertyChanged, and automatically calls DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
        /// </summary>
        /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
        /// <param name="propertyExpression">The property expression. Example: ObservesProperty(() => PropertyName).</param>
        protected internal void ObservesPropertyInternal<T>(Expression<Func<T>> propertyExpression)
        {
            if (this.observedPropertiesExpressions.Contains(propertyExpression.ToString()))
            {
                throw new ArgumentException($"{propertyExpression.ToString()} is already being observed.", nameof(propertyExpression));
            }
            else
            {
                this.observedPropertiesExpressions.Add(propertyExpression.ToString());
                PropertyObserver.Observes(propertyExpression, this.RaiseCanExecuteChanged);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the object is active.
        /// </summary>
        /// <value><see langword="true" /> if the object is active; otherwise <see langword="false" />.</value>
        public bool IsActive
        {
            get
            {
                return this.isActive;
            }

            set
            {
                if (this.isActive != value)
                {
                    this.isActive = value;
                    this.OnIsActiveChanged();
                }
            }
        }

        /// <summary>
        /// Fired if the <see cref="IsActive"/> property changes.
        /// </summary>
        public virtual event EventHandler? IsActiveChanged;

        /// <summary>
        /// This raises the <see cref="DelegateCommandBase.IsActiveChanged"/> event.
        /// </summary>
        protected virtual void OnIsActiveChanged()
        {
            this.IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// An <see cref="ICommand"/> whose delegates do not take any parameters for <see cref="Execute()"/> and <see cref="CanExecute()"/>.
    /// </summary>
    /// <see cref="DelegateCommandBase"/>
    /// <see cref="DelegateCommand{T}"/>
    public class DelegateCommand : DelegateCommandBase
    {
        private Action executeMethod;
        private Func<bool> canExecuteMethod;

        /// <summary>
        /// Creates a new instance of <see cref="DelegateCommand"/> with the <see cref="Action"/> to invoke on execution.
        /// </summary>
        /// <param name="executeMethod">The <see cref="Action"/> to invoke when <see cref="ICommand.Execute(object)"/> is called.</param>
        public DelegateCommand(Action executeMethod)
            : this(executeMethod, () => true)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DelegateCommand"/> with the <see cref="Action"/> to invoke on execution
        /// and a <see langword="Func" /> to query for determining if the command can execute.
        /// </summary>
        /// <param name="executeMethod">The <see cref="Action"/> to invoke when <see cref="ICommand.Execute"/> is called.</param>
        /// <param name="canExecuteMethod">The <see cref="Func{TResult}"/> to invoke when <see cref="ICommand.CanExecute"/> is called.</param>
        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
            : base()
        {
            if (executeMethod == null || canExecuteMethod == null)
            {
                throw new ArgumentNullException(nameof(executeMethod));
            }

            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        /// <summary>
        ///  Executes the command.
        /// </summary>
        public void Execute()
        {
            this.executeMethod();
        }

        /// <summary>
        /// Determines if the command can be executed.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if the command can execute,otherwise returns <see langword="false"/>.</returns>
        public bool CanExecute()
        {
            return this.canExecuteMethod();
        }

        /// <summary>
        /// Handle the internal invocation of <see cref="ICommand.Execute(object)"/>.
        /// </summary>
        /// <param name="parameter">Command Parameter.</param>
        protected override void Execute(object parameter)
        {
            this.Execute();
        }

        /// <summary>
        /// Handle the internal invocation of <see cref="ICommand.CanExecute(object)"/>.
        /// </summary>
        /// <param name="parameter">parameter.</param>
        /// <returns><see langword="true"/> if the Command Can Execute, otherwise <see langword="false" />.</returns>
        protected override bool CanExecute(object parameter)
        {
            return this.CanExecute();
        }

        /// <summary>
        /// Observes a property that implements INotifyPropertyChanged, and automatically calls DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
        /// </summary>
        /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
        /// <param name="propertyExpression">The property expression. Example: ObservesProperty(() => PropertyName).</param>
        /// <returns>The current instance of DelegateCommand.</returns>
        public DelegateCommand ObservesProperty<T>(Expression<Func<T>> propertyExpression)
        {
            this.ObservesPropertyInternal(propertyExpression);
            return this;
        }

        /// <summary>
        /// Observes a property that is used to determine if this command can execute, and if it implements INotifyPropertyChanged it will automatically call DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
        /// </summary>
        /// <param name="canExecuteExpression">The property expression. Example: ObservesCanExecute(() => PropertyName).</param>
        /// <returns>The current instance of DelegateCommand.</returns>
        public DelegateCommand ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
        {
            this.canExecuteMethod = canExecuteExpression.Compile();
            this.ObservesPropertyInternal(canExecuteExpression);
            return this;
        }
    }

    /// <summary>
    /// An <see cref="ICommand"/> whose delegates can be attached for <see cref="Execute(T)"/> and <see cref="CanExecute(T)"/>.
    /// </summary>
    /// <typeparam name="T">Parameter type.</typeparam>
    /// <remarks>
    /// The constructor deliberately prevents the use of value types.
    /// Because ICommand takes an object, having a value type for T would cause unexpected behavior when CanExecute(null) is called during XAML initialization for command bindings.
    /// Using default(T) was considered and rejected as a solution because the implementor would not be able to distinguish between a valid and defaulted values.
    /// <para/>
    /// Instead, callers should support a value type by using a nullable value type and checking the HasValue property before using the Value property.
    /// <example>
    ///     <code>
    /// public MyClass()
    /// {
    ///     this.submitCommand = new DelegateCommand&lt;int?&gt;(this.Submit, this.CanSubmit);
    /// }
    ///
    /// private bool CanSubmit(int? customerId)
    /// {
    ///     return (customerId.HasValue &amp;&amp; customers.Contains(customerId.Value));
    /// }
    ///     </code>
    /// </example>
    /// </remarks>
    public class DelegateCommand<T> : DelegateCommandBase
    {
        private readonly Action<T> executeMethod;
        private Func<T, bool> canExecuteMethod;

        /// <summary>
        /// Initializes a new instance of <see cref="DelegateCommand{T}"/>.
        /// </summary>
        /// <param name="executeMethod">Delegate to execute when Execute is called on the command. This can be null to just hook up a CanExecute delegate.</param>
        /// <remarks><see cref="CanExecute(T)"/> will always return true.</remarks>
        public DelegateCommand(Action<T> executeMethod)
            : this(executeMethod, (o) => true)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DelegateCommand{T}"/>.
        /// </summary>
        /// <param name="executeMethod">Delegate to execute when Execute is called on the command. This can be null to just hook up a CanExecute delegate.</param>
        /// <param name="canExecuteMethod">Delegate to execute when CanExecute is called on the command. This can be null.</param>
        /// <exception cref="ArgumentNullException">When both <paramref name="executeMethod"/> and <paramref name="canExecuteMethod"/> are <see langword="null" />.</exception>
        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
            : base()
        {
            if (executeMethod == null || canExecuteMethod == null)
            {
                throw new ArgumentNullException(nameof(executeMethod));
            }

            TypeInfo genericTypeInfo = typeof(T).GetTypeInfo();

            // DelegateCommand allows object or Nullable<>.
            // note: Nullable<> is a struct so we cannot use a class constraint.
            if (genericTypeInfo.IsValueType)
            {
                if ((!genericTypeInfo.IsGenericType) || (!typeof(Nullable<>).GetTypeInfo().IsAssignableFrom(genericTypeInfo.GetGenericTypeDefinition().GetTypeInfo())))
                {
                    throw new InvalidCastException();
                }
            }

            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        /// <summary>
        /// Executes the command and invokes the <see cref="Action{T}"/> provided during construction.
        /// </summary>
        /// <param name="parameter">Data used by the command.</param>
        public void Execute(T parameter)
        {
            this.executeMethod(parameter);
        }

        /// <summary>
        /// Determines if the command can execute by invoked the <see cref="Func{T,Bool}"/> provided during construction.
        /// </summary>
        /// <param name="parameter">Data used by the command to determine if it can execute.</param>
        /// <returns>
        /// <see langword="true" /> if this command can be executed; otherwise, <see langword="false" />.
        /// </returns>
        public bool CanExecute(T parameter)
        {
            return this.canExecuteMethod(parameter);
        }

        /// <summary>
        /// Handle the internal invocation of <see cref="ICommand.Execute(object)"/>.
        /// </summary>
        /// <param name="parameter">Command Parameter.</param>
        protected override void Execute(object parameter)
        {
            this.Execute((T)parameter);
        }

        /// <summary>
        /// Handle the internal invocation of <see cref="ICommand.CanExecute(object)"/>.
        /// </summary>
        /// <param name="parameter">parameter.</param>
        /// <returns><see langword="true"/> if the Command Can Execute, otherwise <see langword="false" />.</returns>
        protected override bool CanExecute(object parameter)
        {
            return this.CanExecute((T)parameter);
        }

        /// <summary>
        /// Observes a property that implements INotifyPropertyChanged, and automatically calls DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
        /// </summary>
        /// <typeparam name="TType">The type of the return value of the method that this delegate encapulates.</typeparam>
        /// <param name="propertyExpression">The property expression. Example: ObservesProperty(() => PropertyName).</param>
        /// <returns>The current instance of DelegateCommand.</returns>
        public DelegateCommand<T> ObservesProperty<TType>(Expression<Func<TType>> propertyExpression)
        {
            this.ObservesPropertyInternal(propertyExpression);
            return this;
        }

        /// <summary>
        /// Observes a property that is used to determine if this command can execute, and if it implements INotifyPropertyChanged it will automatically call DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
        /// </summary>
        /// <param name="canExecuteExpression">The property expression. Example: ObservesCanExecute(() => PropertyName).</param>
        /// <returns>The current instance of DelegateCommand.</returns>
        public DelegateCommand<T> ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
        {
            Expression<Func<T, bool>> expression = Expression.Lambda<Func<T, bool>>(canExecuteExpression.Body, Expression.Parameter(typeof(T), "o"));
            this.canExecuteMethod = expression.Compile();
            this.ObservesPropertyInternal(canExecuteExpression);
            return this;
        }
    }

    /// <summary>
    /// Implementation of <see cref="INotifyPropertyChanged"/> to simplify models.
    /// </summary>
    public abstract class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            this.RaisePropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="onChanged">Action that is called after the property value has been changed.</param>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            onChanged?.Invoke();
            this.RaisePropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        protected void RaisePropertyChanged([CallerMemberName]string? propertyName = null)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="args">The PropertyChangedEventArgs.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            this.PropertyChanged?.Invoke(this, args);
        }
    }
}
