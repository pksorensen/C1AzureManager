using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace S_Innovations.C1.AzureManager.MVVM
{


   
       /// <summary>
        /// Base class for MVVM pattern view models.
        /// </summary>
        public abstract class ViewModel : INotifyPropertyChanged, IDisposable
        {
            private IDictionary<string, object> _propertyValues;

            public event PropertyChangedEventHandler PropertyChanged;

            protected void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception e) // Some unexpected exceptions occur.
                    {
                        Debug.WriteLine("ScrollIntoView failed");
                        Debug.WriteLine(e);
                    }
                }
            }

            protected void RaisePropertyChanged<TProperty>(Expression<Func<TProperty>> property)
            {
                RaisePropertyChanged(PropertyName(property));
            }

            public virtual void Dispose()
            {
            }

            /// <summary>
            /// Implements a model property setter that fires <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
            /// Allows to store the property value in a field.
            /// </summary>
            /// <typeparam name="TProperty">Type of the property.</typeparam>
            /// <param name="field">Reference to a property with both getter and setter.</param>
            /// <param name="newValue">Desired value for the property.</param>
            /// <param name="propertyName">Name of the property used to notify listeners. This value is optional and can be provided automatically when invoked from compilers that support CallerMemberName.</param>
            /// <returns>True if the value was changed, false if the existing value matched the desired value.</returns>
            protected bool OnPropertyChange<TProperty>(ref TProperty field, TProperty newValue, [CallerMemberName] string propertyName = null)
            {
                if (Equals(field, newValue)) return false;

                field = newValue;
                RaisePropertyChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Implements a model property setter that fires <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
            /// Uses a dictionary to store a property value.
            /// </summary>
            /// <typeparam name="TProperty">Type of the property.</typeparam>
            /// <param name="newValue">Desired value for the property.</param>
            /// <param name="propertyName">Name of the property used to notify listeners. This value is optional and can be provided automatically when invoked from compilers that support CallerMemberName.</param>
            /// <returns>True if the value was changed, false if the existing value matched the desired value.</returns>
            protected bool OnPropertyChange<TProperty>(TProperty newValue, [CallerMemberName] string propertyName = null)
            {
                object oldValue;
                PropertyValues.TryGetValue(propertyName, out oldValue);

                if (Equals(oldValue, newValue))
                    return false;

                PropertyValues[propertyName] = newValue;
                RaisePropertyChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Gets a property value that has not a backing field. Values are stored in the dictionary.
            /// </summary>
            /// <typeparam name="TProperty">The property type.</typeparam>
            /// <param name="propertyName">The property name. This value is optional and can be provided automatically when invoked from compilers that support CallerMemberName.</param>
            /// <returns>The stored property value.</returns>
            protected TProperty GetPropertyValue<TProperty>([CallerMemberName] string propertyName = null)
            {
                object oldValue;
                PropertyValues.TryGetValue(propertyName, out oldValue);
                return (TProperty)(oldValue ?? default(TProperty));
            }

            /// <summary>
            /// Implements a model property setter that fires <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
            /// Allows to store the property value in a custom store.
            /// </summary>
            /// <typeparam name="TProperty">The property type.</typeparam>
            /// <param name="newValue">A new property value.</param>
            /// <param name="setter">Saves the new value to a storage.</param>
            /// <param name="propertyName">The property name. This value is optional and can be provided automatically when invoked from compilers that support CallerMemberName.</param>
            /// <returns><c>True</c> if property value was changed, otherwise <c>False</c>.</returns>
            /// <example>
            ///	public bool IsFullScreen
            ///	{
            ///		get { bool isFullScreen; _dictionary.TryGetValue("IsFullScreen", out isFullScreen); return isFullScreen; }
            ///		set { OnPropertyChange(() => IsFullScreen, value, newValue => _dictionary["IsFullScreen"] = newValue); }
            ///	}
            /// </example>
            protected bool OnPropertyChange<TProperty>(TProperty newValue, Action setter, [CallerMemberName] string propertyName = null)
            {
                var oldValue = GetType().GetTypeInfo().GetDeclaredProperty(propertyName).GetValue(this);
                if (Equals(oldValue, newValue))
                    return false;

                setter();
                RaisePropertyChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Implements a model property setter that fires <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
            /// Allows to store the property value in a field.
            /// </summary>
            /// <typeparam name="TProperty">The property type.</typeparam>
            /// <param name="property">The labda expression that contains only the property accessor e.g. <c>()=>PropertyName</c>.</param>
            /// <param name="field">The field that stores the property value.</param>
            /// <param name="newValue">A new property value.</param>
            /// <returns><c>True</c> if property value was changed, otherwise <c>False</c>.</returns>
            /// <example>
            ///	private bool _isFullScreen;
            ///	public bool IsFullScreen
            ///	{
            ///		get { return _isFullScreen; }
            ///		set { OnPropertyChange(() => IsFullScreen, ref _isFullScreen, value); }
            ///	}
            /// </example>
            protected bool OnPropertyChange<TProperty>(Expression<Func<TProperty>> property, ref TProperty field, TProperty newValue)
            {
                if (Equals(field, newValue))
                    return false;

                field = newValue;
                var propertyName = PropertyName(property);
                RaisePropertyChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Implements a model property setter that fires <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
            /// Allows to store the property value in a custom store.
            /// </summary>
            /// <typeparam name="TProperty">The property type.</typeparam>
            /// <param name="property">The labda expression that contains only the property accessor e.g. <c>()=>PropertyName</c>.</param>
            /// <param name="newValue">A new property value.</param>
            /// <param name="setter">Saves the new value to a storage.</param>
            /// <returns><c>True</c> if property value was changed, otherwise <c>False</c>.</returns>
            /// <example>
            ///	public bool IsFullScreen
            ///	{
            ///		get { bool isFullScreen; _dictionary.TryGetValue("IsFullScreen", out isFullScreen); return isFullScreen; }
            ///		set { OnPropertyChange(() => IsFullScreen, value, newValue => _dictionary["IsFullScreen"] = newValue); }
            ///	}
            /// </example>
            protected bool OnPropertyChange<TProperty>(Expression<Func<TProperty>> property, TProperty newValue, Action setter)
            {
                var oldValue = property.Compile()();
                if (Equals(oldValue, newValue))
                    return false;

                setter();
                var propertyName = PropertyName(property);
                RaisePropertyChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Sets a property value that has not a backing field. Values are stored in the dictionary.
            /// </summary>
            /// <typeparam name="TProperty">The property type.</typeparam>
            /// <param name="property">The labda expression that contains only the property accessor e.g. <c>()=>PropertyName</c>.</param>
            /// <param name="newValue">A new property value.</param>
            /// <returns><c>True</c> if property value was changed, otherwise <c>False</c>.</returns>
            /// <example>
            ///	public bool IsFullScreen
            ///	{
            ///		get { return GetPropertyValue(() => IsFullScreen); }
            ///		set { SetPropertyValue(() => IsFullScreen, value); }
            ///	}
            /// </example>
            protected bool SetPropertyValue<TProperty>(Expression<Func<TProperty>> property, TProperty newValue)
            {
                var propertyName = PropertyName(property);

                object oldValue;
                PropertyValues.TryGetValue(propertyName, out oldValue);

                if (Equals(oldValue, newValue))
                    return false;

                PropertyValues[propertyName] = newValue;
                RaisePropertyChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Gets a property value that has not a backing field. Values are stored in the dictionary.
            /// </summary>
            /// <typeparam name="TProperty">The property type.</typeparam>
            /// <param name="property">The labda expression that contains only the property accessor e.g. <c>()=>PropertyName</c>.</param>
            /// <returns>The stored property value.</returns>
            protected TProperty GetPropertyValue<TProperty>(Expression<Func<TProperty>> property)
            {
                var propertyName = PropertyName(property);
                object oldValue;
                PropertyValues.TryGetValue(propertyName, out oldValue);
                return (TProperty)(oldValue ?? default(TProperty));
            }

            /// <summary>
            /// Gets a property value, initializes it with a new value if required and sibscrebes to change events.
            /// </summary>
            /// <typeparam name="TItem">The collection item type.</typeparam>
            /// <param name="collectionField">A reference to the field that keeps the property value.</param>
            /// <param name="collectionChangedCallback">The delegate that will suscribed to collection change events.</param>
            /// <returns>The collection.</returns>
            protected ObservableCollection<TItem> GetCollectionPropertyLazyValue<TItem>(ref ObservableCollection<TItem> collectionField,
                                                                                  NotifyCollectionChangedEventHandler collectionChangedCallback)
            {
                if (collectionField == null)
                {
                    collectionField = new ObservableCollection<TItem>();
                    collectionField.CollectionChanged += collectionChangedCallback;
                }
                return collectionField;
            }

            /// <summary>
            /// Sets a new value to a collection property with notification and subscribes to its changes.
            /// </summary>
            /// <typeparam name="TItem">The collection item type.</typeparam>
            /// <param name="collectionField">A reference to the field that keeps the property value.</param>
            /// <param name="collectionChangedCallback">The delegate that will suscribed to collection change events.</param>
            /// <param name="newValue">A new collection.</param>
            /// <param name="propertyName">The property name.</param>
            protected void OnCollectionPropertyChange<TItem>(ref ObservableCollection<TItem> collectionField,
                                                             ObservableCollection<TItem> newValue,
                                                             NotifyCollectionChangedEventHandler collectionChangedCallback,
                                                             [CallerMemberName] string propertyName = null)
            {
                var old = collectionField;
                if (OnPropertyChange(ref collectionField, newValue, propertyName))
                {
                    if (old != null)
                        old.CollectionChanged -= collectionChangedCallback;

                    if (collectionField != null)
                        collectionField.CollectionChanged += collectionChangedCallback;
                }
            }

            protected static string PropertyName<TProperty>(Expression<Func<TProperty>> property)
            {
                if (property == null)
                    throw new ArgumentNullException("property");

                var memberExpression = property.Body as MemberExpression;
                if (memberExpression == null)
                    throw new ArgumentException("A property value is expected.", "property");

                var propertyName = memberExpression.Member.Name;
                return propertyName;
            }

            private IDictionary<string, object> PropertyValues
            {
                get { return _propertyValues = (_propertyValues ?? new Dictionary<string, object>()); }
            }
        }
    

}
