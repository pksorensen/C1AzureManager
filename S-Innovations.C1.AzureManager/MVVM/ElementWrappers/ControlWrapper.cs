using System;
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
            get { return Elements.First().Dispatcher; }
        }
    }
}
