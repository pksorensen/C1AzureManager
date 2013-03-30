using System.Windows;


namespace S_Innovations.C1.AzureManager.MVVM.ElementWrappers
{

        public static class ElementBinder
        {
            public static readonly DependencyProperty WrapperProperty =
                DependencyProperty.RegisterAttached("Wrapper", typeof(object), typeof(ElementBinder),
                new PropertyMetadata(null, OnWrapperChanged));

            private static void OnWrapperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var element = (FrameworkElement)d;

                var wrapper = e.OldValue as IElementWrapper;
                if (wrapper != null)
                {
                    wrapper.RemoveElement(d);
                    element.Unloaded -= ElementOnUnloaded;
                    element.Loaded -= ElementOnLoaded;
                }

                wrapper = e.NewValue as IElementWrapper;

                if (wrapper != null)
                {
                    wrapper.AddElement(d);
                    element.Unloaded += ElementOnUnloaded;
                    element.Loaded += ElementOnLoaded;
                }
            }

            public static void SetWrapper(DependencyObject element, object value)
            {
                element.SetValue(WrapperProperty, value);
            }

            public static object GetWrapper(DependencyObject element)
            {
                return element.GetValue(WrapperProperty);
            }

            private static void ElementOnUnloaded(object sender, RoutedEventArgs routedEventArgs)
            {
                var wrapper = GetWrapper((DependencyObject)sender) as IElementWrapper;
                if (wrapper != null)
                {
                    // Avoid memory leaks.
                    wrapper.RemoveElement(sender);
                }
            }

            private static void ElementOnLoaded(object sender, RoutedEventArgs e)
            {
                var wrapper = GetWrapper((DependencyObject)sender) as IElementWrapper;
                if (wrapper != null && !wrapper.HasElement(sender))
                {
                    // Restoring element value.
                    wrapper.AddElement(sender);
                }
            }
        }
    
}
