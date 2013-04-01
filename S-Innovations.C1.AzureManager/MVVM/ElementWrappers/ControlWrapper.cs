using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace S_Innovations.C1.AzureManager.MVVM.ElementWrappers
{
    public class ControlWrapper<T> : ElementWrapper<T> where T : Control
    {
        public bool IsVisual()
        {
            return Elements.Count > 0;
        }
        public Dispatcher Dispatcher
        {
            get { var f = Elements.FirstOrDefault();
                if (f==null)
                return null;
                return f.Dispatcher; }
        }
    }
    public class ListViewWrapper<T> : ControlWrapper<ListView> where T : class
    {
        public IList SelectedItems
        {
            get { return this.Elements.First().SelectedItems; }
        }
        public T SelectedItem
        {
            get { return this.Elements.First().SelectedItem as T; }
        }
    }
}
