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

        #region Fields

        private const string AZURE_STORAGE_CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";
        private string _status;
        private bool disposed = false;
        private XDocument _websites;
        private XDocument _perf;
        private TabItemViewModel _selected_tab = null;
        private List<C1Container> _deployments = null;

        #endregion

        #region Properties

        public XDocument WebSitesConfiguration 
        { 
            get { return _websites; }
            set {
                    OnPropertyChange(ref _websites, value);
            }
        }

        public XDocument PerformanceCountersConfiguration
        {
            get { return _perf; }
            set
            {
                OnPropertyChange(ref _perf, value);
            }
        }

        public TabItemViewModel OpenedTab
        {
            get { return _selected_tab; }
            set
            {
                if (_selected_tab != null)
                    _selected_tab.RaiseDeActiveEvent();
                _selected_tab = value;
                _selected_tab.RaiseActiveEvent();
            }
        }
        public List<C1Container> Deployments
        {
            get { return _deployments; }
            set
            {

                if (_deployments != null)
                {
                    //dispose logfolders.
                    foreach (var o in _deployments.Where(c => c.LogFolderLazyAsync.IsValueCreated))
                        o.LogFolderLazyAsync.Value.Result.Dispose();
                }
                OnPropertyChange(ref _deployments, value);
                OpenLogsCommand.RaiseCanExecuteChanged();
            }
        }


        public String Status { get { return _status; } set { OnPropertyChange(ref _status, value); } }
        public String StorageAccount { get; set; }
        public String StorageKey { get; set; }
        public String ConfigurationFolder { get; set; }  
        public ICommand OpenConnectionCommand { get; private set; }
        public EventCommand<IList> OpenLogsCommand { get; private set; }
        public EventCommand<IList> ManagerWebsitesCommand { get; private set; }
        public EventCommand<object> DeploymentSelectionChangedCommand { get; private set; }
        public ICommand CloseTabCommand { get; private set; }
        public ObservableCollection<TabItemViewModel> TabItemsViewModelCollection { get; set; }

        #endregion

        #region Constructors

        public MainWindowViewModel()
        {
            OpenConnectionCommand = new RelayCommand(OpenConnection);
            OpenLogsCommand = new EventCommand<IList>(open_logs, x => x.Count > 0);
            ManagerWebsitesCommand = new EventCommand<IList>(ManageWebsites, x => x.Count > 0);            
            TabItemsViewModelCollection = new ObservableCollection<TabItemViewModel>();
            CloseTabCommand = new EventCommand<TabItemViewModel>(close_tabs);
            DeploymentSelectionChangedCommand = new EventCommand<object>(k => { OpenLogsCommand.RaiseCanExecuteChanged(); });
           //  public RelayCommand CloseTabCommand { get; set; }
            
            //TODO Fix the watermarks.
            StorageAccount = "Azure Storage Account";
            StorageKey = "Key";
            ConfigurationFolder = "Not Used";

            //LogViewer = new TextEditorWrapper();


            LoadConnectionInfo();
        }

        #endregion

        #region IDisposable

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
        
        #endregion
        
        #region Privates Methods
        private void CloseAllTabs()
        {
            foreach (var o in TabItemsViewModelCollection.ToArray())
                close_tabs(o);
        }
        private void StoreConnectionInfo()
        {
            try
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
            catch
            {
                Status = "Could not save cache connection file";
            }

        }
        private void LoadConnectionInfo()
        {
            if (File.Exists("cache.xml"))
            {
                try
                {
                    XElement x = XElement.Load("cache.xml");
                    x = x.Element("Connection");
                    StorageAccount = x.Attribute("StorageAccount").Value;
                    StorageKey = x.Attribute("StorageKey").Value;
                    ConfigurationFolder = x.Attribute("ConfigurationFolder").Value;
                    Status = "Loaded connection info from cache.xml";
                }
                catch
                {
                    Status = "Error Loading connection cache file.";
                }
            }
        }
        private async void OpenConnection()
        {
            // Deployments = new List<C1Container>();
            CloseAllTabs(); Status = "Closing Open Tabs...";

            var ConnectionString =
                string.Format(AZURE_STORAGE_CONNECTION_STRING, StorageAccount, StorageKey);
            Status = "Finding Deployments ...";
            Deployments = await Task.Run<List<C1Container>>(() =>
            {

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
                StoreConnectionInfo(); //Cache.xml

                var blobclient = storageAccount.CreateCloudBlobClient();
                return (blobclient.ListContainers()
                    .Select(c => new C1Container { Container = c, ContainerType = c.CompositeContainerType(), Name = c.Name })
                    .Where(c => c.ContainerType != CompositeContainerType.None)
                    .GroupByDeployments()).ToList();
            });
            Status = "Found " + Deployments.Count + " Deployments";
        }
        private async void ManageWebsites(IList a)
        {

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
                TabItemsViewModelCollection.Add(added = new DeploymentLogViewerViewModel(c1));
                OpenedTab = added;
                RaisePropertyChanged("OpenedTab");
                await added.Start();
            }
            Status = "Logs have been opened";

            // tab.
            //TabControlWrapper.Element.Items.Add();


        }
        //private async void test_connection()
        //{
        //    var ConnectionString =
        //        string.Format(AZURE_STORAGE_CONNECTION_STRING, StorageAccount, StorageKey);


        //    await Task.Run(() =>
        //    {
        //        try
        //        {

        //            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
        //            var blobclient = storageAccount.CreateCloudBlobClient();
        //            var container = blobclient.GetContainerReference(ConfigurationFolder);
        //            container.Exists(); //Verify the login works.

        //            // If no exception have been thrown, the information was valid. 
        //            StoreConnectionInfo();


        //            var websites = new ConfigurationFile(container, "Configuration/Websites.xml");
        //            WebSitesConfiguration = XDocument.Load(websites.Stream);
        //            var performens = new ConfigurationFile(container, "Configuration/PerformanceCountersConfiguration.xml");
        //            PerformanceCountersConfiguration = XDocument.Load(performens.Stream);




        //        }
        //        catch (Exception e)
        //        {

        //        }


        //    });


        //}

        
        #endregion
    }
}
