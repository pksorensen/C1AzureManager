using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S_Innovations.C1.AzureManager.MVVM.ElementWrappers
{
    /// <summary>
    /// Allows to receive a <see cref="DependencyObject"/>.
    /// </summary>
    public interface IElementWrapper
    {
        bool HasElement(object d);
        void AddElement(object d);
        void RemoveElement(object d);
    }
}
