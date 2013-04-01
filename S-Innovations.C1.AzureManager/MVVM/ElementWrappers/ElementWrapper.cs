using System;
using System.Collections.Generic;
using System.Linq;

namespace S_Innovations.C1.AzureManager.MVVM.ElementWrappers
{

        public class ElementWrapper<TElement> : IElementWrapper
        {
            private ISet<TElement> _elements;

            protected ISet<TElement> Elements
            {
                get { return _elements ?? (_elements = new HashSet<TElement>()); }
            }
            public TElement Element { get { return Elements.FirstOrDefault(); } }

            protected virtual void OnElementAdded(TElement element)
            {
            }

            protected virtual void OnElementRemoved(TElement element)
            {
            }

            #region Implementation of IElementWrapper

            public bool HasElement(object d)
            {
                return _elements.Contains((TElement)d);
            }

            public void AddElement(object d)
            {
                var element = (TElement)d;
                Elements.Add(element);
                OnElementAdded(element);
            }

            public void RemoveElement(object d)
            {
                var element = (TElement)d;
                Elements.Remove(element);
                OnElementRemoved(element);
            }

            #endregion
        }
    
}
