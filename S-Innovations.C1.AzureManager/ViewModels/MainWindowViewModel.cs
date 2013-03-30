using ICSharpCode.AvalonEdit.Document;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using S_Innovations.C1.AzureManager.Models;
using S_Innovations.C1.AzureManager.MVVM;
using S_Innovations.C1.AzureManager.MVVM.EventCommands;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;


namespace S_Innovations.C1.AzureManager.ViewModels
{  
    
  

    public class WebsitesConfiguration : ConfigurationFile
    {
        public WebsitesConfiguration(CloudBlobContainer DeploymentContainer) : base(DeploymentContainer,"Websites.xml")
        {

        }

    }

    public class MainWindowViewModel : ViewModel
    {

        private const string AZURE_STORAGE_CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";


        public String StorageAccount { get; set; }
        public String StorageKey { get; set; }
        public String ConfigurationFolder { get;  set; }

        private XDocument _websites;
        public XDocument WebSitesConfiguration 
        { 
            get { return _websites; }
            set {
                    OnPropertyChange(ref _websites, value);
            }
        }
        private XDocument _perf;
        public XDocument PerformanceCountersConfiguration
        {
            get { return _perf; }
            set
            {
                OnPropertyChange(ref _perf, value);
            }
        }
        public TextDocument FileText { get; set; }

        public ICommand TestConnectionCommand { get; private set; }

        public TextEditorWrapper LogViewer { get; set; }
        public MainWindowViewModel()
        {
            TestConnectionCommand = new RelayCommand(test_connection);
            LogViewer = new TextEditorWrapper();

            if (File.Exists("cache.xml"))
            {
                XElement x = XElement.Load("cache.xml");
                x = x.Element("Connection");
                StorageAccount = x.Attribute("StorageAccount").Value;
                StorageKey = x.Attribute("StorageKey").Value;
                ConfigurationFolder = x.Attribute("ConfigurationFolder").Value;
            }
        }

        TextEditorUpdater LogUpdater = null;
        private async void test_connection()
        {
            var ConnectionString =
                string.Format(AZURE_STORAGE_CONNECTION_STRING, StorageAccount, StorageKey);
            

            await Task.Run(() =>
            {
                try
                {

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
                    var blobclient = storageAccount.CreateCloudBlobClient();
                    var container = blobclient.GetContainerReference(ConfigurationFolder);
                    container.Exists(); //Verify the login works.

                    // If no exception have been thrown, the information was valid. 
                    StoreConnectionInfo();

                    
                    var websites = new ConfigurationFile(container, "Configuration/Websites.xml");
                    WebSitesConfiguration = XDocument.Load(websites.Stream);
                    var performens = new ConfigurationFile(container, "Configuration/PerformanceCountersConfiguration.xml");
                    PerformanceCountersConfiguration = XDocument.Load(performens.Stream);


                    LogUpdater = new TextEditorUpdater(new LogFolder(container, "Diagnostics/WebRoleLogs"), LogViewer);
          
                }
                catch (Exception e)
                {

                }

              
            });
          

        }



        private void StoreConnectionInfo()
        {
            XDocument e = new XDocument(new XDeclaration("1.0", "UTF8", "yes"));
            XElement Root = new XElement("Root");
            XElement connection = new XElement("Connection");
            Root.Add(connection);
            e.Add(Root);
            connection.Add(new XAttribute("StorageAccount", StorageAccount));
            connection.Add(new XAttribute("StorageKey", StorageKey));
            connection.Add(new XAttribute("ConfigurationFolder", ConfigurationFolder));
            e.Save("cache.xml");
        }
    }
}
