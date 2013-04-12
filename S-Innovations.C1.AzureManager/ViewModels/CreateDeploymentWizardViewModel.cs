using S_Innovations.C1.AzureManager.TemplateSelectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace S_Innovations.C1.AzureManager.ViewModels
{
    public class CreateDeploymentWizardViewModel : TabItemViewModel
    {
        #region private
        string _deploymentFile;
        string _configurationFile;
        

        #endregion
        #region Commands

        #endregion

        #region Properties

        private XDocument _configuration;
        private IEnumerable<XElement> _settings; 


        public XDocument Configuration
        {
            get { return _configuration; }
            set {


                OnPropertyChange<XDocument>(ref _configuration, value);

                var Role = Configuration.Root.Element("{http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration}Role");
                var Instance = Role.Element("{http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration}Instance");
                var ConfigurationSettings = Role.Element("{http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration}ConfigurationSettings");
                Settings = ConfigurationSettings.Elements("{http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration}Setting");
                RaisePropertyChanged("DeploymentName");
                RaisePropertyChanged("DefaultWebsiteName");
                RaisePropertyChanged("DisplayName");
            }
        }
        public IEnumerable<XElement> Settings 
        {
            get{return _settings;}
            set { OnPropertyChange(ref _settings, value); }
        }
        //public IEnumerable<XElement> Settings { get; set; }

      

        public string DeploymentName
        {
            get { return Settings == null ? String.Empty : Settings.FirstOrDefault(n => n.FirstAttribute.Value == "DeploymentName").LastAttribute.Value; }
            set { Settings.FirstOrDefault(n => n.FirstAttribute.Value == "DeploymentName").LastAttribute.Value = value; RaisePropertyChanged("DeploymentName"); }
        }
        public string DefaultWebsiteName
        {
            get { return Settings == null ? String.Empty : Settings.FirstOrDefault(n => n.FirstAttribute.Value == "DefaultWebsiteName").LastAttribute.Value; }
            set { Settings.FirstOrDefault(n => n.FirstAttribute.Value == "DefaultWebsiteName").LastAttribute.Value = value; RaisePropertyChanged("DefaultWebsiteName"); }
        }
        public string DisplayName
        {
            get { return Settings==null?String.Empty: Settings.FirstOrDefault(n => n.FirstAttribute.Value == "DisplayName").LastAttribute.Value; }
            set { Settings.FirstOrDefault(n => n.FirstAttribute.Value == "DisplayName").LastAttribute.Value = value; RaisePropertyChanged("DisplayName"); }
        }
        

        #endregion

        public CreateDeploymentWizardViewModel(string configurationFile, string deploymentFile)
            : base(TabControlType.C1DeploymentWizard, "Create Deployment")
        {
            _deploymentFile = deploymentFile;
            _configurationFile = configurationFile;
        }

        public override async void RaiseActiveEvent()
        {
           // await Task.Delay(10000);
            Configuration = await Task.Run<XDocument>(() =>
            {
                try
                {
                  
                    return XDocument.Load(_configurationFile);
                }
                catch (Exception ex)
                {

                }
                return null;
            });

           


        }

    }
}
