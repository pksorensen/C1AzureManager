using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace S_Innovations.C1.AzureManager.MVVM.EventCommands
{
    public static class EventBinding
    {


        #region SelectionChanged

        public static readonly DependencyProperty SelectionChangedProperty =
            DependencyProperty.RegisterAttached("SelectionChanged", typeof(ICommand), typeof(EventBinding),
            new PropertyMetadata(default(DependencyObject), OnSelectionChangedPropertyChanged));

        public static void SetSelectionChanged(DependencyObject d, ICommand value)
        {
            d.SetValue(SelectionChangedProperty, value);
        }

        public static ICommand GetSelectionChanged(DependencyObject d)
        {
            return (ICommand)d.GetValue(SelectionChangedProperty);
        }

        private static void OnSelectionChangedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SubscribeToEvent<Selector>(d, e, el => el.SelectionChanged += OnSelectionChanged, el => el.SelectionChanged -= OnSelectionChanged);
        }

        static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selector = (Selector)sender;
            var command = GetSelectionChanged(selector);
            command.Execute(selector.SelectedValue);
          
        }

        #endregion





        private static void SubscribeToEvent<TElement>(DependencyObject d, DependencyPropertyChangedEventArgs e, Action<TElement> subscribe, Action<TElement> unsubscribe) where TElement : class
        {
            var element = d as TElement;
            if (element != null)
            {
                if (e.OldValue != null)
                    unsubscribe(element);

                if (e.NewValue != null)
                    subscribe(element);
            }
        }

    }
}
