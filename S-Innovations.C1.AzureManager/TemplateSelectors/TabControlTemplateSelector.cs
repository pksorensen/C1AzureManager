using S_Innovations.C1.AzureManager.MVVM;
using S_Innovations.C1.AzureManager.MVVM.EventCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace S_Innovations.C1.AzureManager.TemplateSelectors
{
    //public class TabItemViewModel<T>  : TabItemViewModel where T : TabControlType
    //{
    //    public TabItemViewModel() : base(default(T),"test")
    //    {

    //    }
    //}
    public abstract class TabItemViewModel : ViewModel
    {
        public TabControlType TabType { get; set; }

        public String Header { get; set; }
        public TabItemViewModel(TabControlType type, string header = "Test")
        {
            Header = header;
            TabType = type;
     
        }
        ~TabItemViewModel()
        {

        }
       // public event EventHandler Active;
       // public event EventHandler DeActive;
        public virtual void RaiseActiveEvent()
        {
          //  if (Active != null)
          //      Active(this, EventArgs.Empty);
        }
        public virtual void RaiseDeActiveEvent()
        {
         ///   if (DeActive != null)
          //      DeActive(this, EventArgs.Empty);
        }
        public virtual void RaiseRemoved()
        {

        }
        


    }
    public enum TabControlType
    {
        NONE = 0,
        C1DeploymentLogViewer = 1,
        C1DeploymentWebsites = 2,
        C1DeploymentWizard = 3,
    }
    public class TabControlTemplateSelector : DataTemplateSelector
    {
        public DataTemplate C1DeploymentLogViewer { get; set; }
        public DataTemplate C1DeploymentWebsites { get; set; }
        public DataTemplate C1DeploymentWizard { get; set; }

        public override DataTemplate SelectTemplate(Object item,
        DependencyObject container)
        {
            var vm = item as TabItemViewModel;

            if (vm == null) return base.SelectTemplate(item, container);
            
            switch (vm.TabType)
            {
                case TabControlType.C1DeploymentLogViewer:
                    return C1DeploymentLogViewer;
                case TabControlType.C1DeploymentWebsites:
                    return C1DeploymentWebsites;
                case TabControlType.C1DeploymentWizard:
                    return C1DeploymentWizard;
                default:
                    return base.SelectTemplate(item, container);
            }
                 
        }
    }
}
