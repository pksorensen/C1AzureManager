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
using System.Linq;
using S_Innovations.C1.AzureManager.ExtensionMethods;
using System.Collections.Generic;
using S_Innovations.C1.AzureManager.MVVM.ElementWrappers;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using S_Innovations.C1.AzureManager.TemplateSelectors;
using System.Collections;

namespace S_Innovations.C1.AzureManager.ViewModels
{  
    
  



    public class MainWindowViewModel : ViewModel, IDisposable
    {

        private const string AZURE_STORAGE_CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";
        private string _status;

        public String Status { get { return _status; } set { OnPropertyChange(ref _status, value); } }
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
        public EventCommand<IList> OpenLogsCommand { get; private set; }
        public EventCommand<IList> ManagerWebsitesCommand { get; private set; }
        public EventCommand<object> DeploymentSelectionChangedCommand { get; private set; }
        public ICommand CloseTabCommand { get; private set; }
        
        private TabItemViewModel _selected_tab = null;
        public TabItemViewModel OpenedTab { get{ return _selected_tab;}
            set { 
                if(_selected_tab!=null)
                    _selected_tab.RaiseDeActiveEvent();
                _selected_tab = value;
                _selected_tab.RaiseActiveEvent();
            }
        }
        public ObservableCollection<TabItemViewModel> TabItemsViewModelCollection { get; set; }

        public TextEditorWrapper LogViewer { get; set; }
        public MainWindowViewModel()
        {
            TestConnectionCommand = new RelayCommand(test_storage_connection);
            OpenLogsCommand = new EventCommand<IList>(open_logs, x => x.Count > 0);
            ManagerWebsitesCommand = new EventCommand<IList>(manage_websites, x => x.Count > 0);            
            TabItemsViewModelCollection = new ObservableCollection<TabItemViewModel>();
            CloseTabCommand = new EventCommand<TabItemViewModel>(close_tabs);
            DeploymentSelectionChangedCommand = new EventCommand<object>(k => { OpenLogsCommand.RaiseCanExecuteChanged(); });
           //  public RelayCommand CloseTabCommand { get; set; }
            
            //TODO Fix the watermarks.
            StorageAccount = "Azure Storage Account";
            StorageKey = "Key";
            ConfigurationFolder = "Not Used";

            LogViewer = new TextEditorWrapper();
           

            if (File.Exists("cache.xml"))
            {
                XElement x = XElement.Load("cache.xml");
                x = x.Element("Connection");
                StorageAccount = x.Attribute("StorageAccount").Value;
                StorageKey = x.Attribute("StorageKey").Value;
                ConfigurationFolder = x.Attribute("ConfigurationFolder").Value;
                Status = "Loaded connection info from cache.xml";
            }
        }


        private void close_tabs(TabItemViewModel o)
        {
            TabItemsViewModelCollection.Remove(o);
            o.RaiseRemoved();
        }
        

        private async void open_logs(IList a)
        {
            Status = "Opening the Log, please wait for initial download...";
            //var t = DeployedListView.SelectedItem;
            DeploymentLogViewerViewModel added = null;
            foreach (C1Container c1 in a)
            {
                TabItemsViewModelCollection.Add(added= new DeploymentLogViewerViewModel(c1));
                OpenedTab = added;
                RaisePropertyChanged("OpenedTab");
                await added.Start();
            }
            Status = "Logs have been opened";
            
           // tab.
            //TabControlWrapper.Element.Items.Add();
            

        }
        private async void manage_websites(IList a)
        {
           
        }
        

        List<C1Container> _deployments = null;
        public  List<C1Container> Deployments {get{return _deployments;}
            set
            {
                if (_deployments != null)
                {
                    foreach (var o in _deployments)
                        o.LogFolderAsync.Result.Dispose();
                }
                OnPropertyChange(ref _deployments, value);
                OpenLogsCommand.RaiseCanExecuteChanged();
            }
        }//= new List<C1Container>();
        private void close_all_tabs()
        {
            foreach (var o in TabItemsViewModelCollection.ToArray())
                close_tabs(o);
        }
        private async void test_storage_connection()
        {
           // Deployments = new List<C1Container>();
            close_all_tabs(); Status = "Closing Open Tabs...";

            var ConnectionString =
                string.Format(AZURE_STORAGE_CONNECTION_STRING, StorageAccount, StorageKey);
            Status = "Finding Deployments ...";
            Deployments = await Task.Run < List<C1Container>>(() =>
            {

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
                    StoreConnectionInfo(); //Cache.xml

                    var blobclient = storageAccount.CreateCloudBlobClient();
                    return ( blobclient.ListContainers()
                        .Select(c => new C1Container {Container = c, ContainerType=c.CompositeContainerType(), Name=c.Name})
                        .Where(c=>c.ContainerType != CompositeContainerType.None)
                        .GroupByDeployments()).ToList(); 
            });
            Status = "Found " + Deployments.Count + " Deployments";
        }

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


                    
          
                }
                catch (Exception e)
                {

                }

              
            });
          

        }

        private bool disposed = false;
        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                 //   close_all_tabs();
                    Deployments = null;
                }
                disposed = true;
            }
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
