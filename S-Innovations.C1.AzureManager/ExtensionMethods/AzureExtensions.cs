using Microsoft.WindowsAzure.Storage.Blob;
using S_Innovations.C1.AzureManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ContainerType = S_Innovations.C1.AzureManager.ExtensionMethods.CompositeContainerType;

namespace S_Innovations.C1.AzureManager.ExtensionMethods
{
    public enum CompositeContainerType
    {
        None = 0,
        Website = 1,
        Deployment = 2
    }
    public enum CompositeDeploymentStatus
    {
        Running = 0,
        Stopped = 1,
    }
    public struct AzureInstance
    {
        public CompositeDeploymentStatus Status { get; set; }
    }
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory)) { }

        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap()) { }

        public TaskAwaiter<T> GetAwaiter() { return Value.GetAwaiter(); }
    }
    public class C1Container
    {
        public CompositeContainerType ContainerType { get; set; }
        public CloudBlobContainer Container { get; set; }
        private AsyncLazy<LogFolder> _logfolder;

        private async Task<LogFolder> load_logfolder()
        {

                var f = new LogFolder(Container, "Diagnostics/WebRoleLogs");
                await f.Start();
                return f;
            
        }

        public C1Container()
        {
            _logfolder = new AsyncLazy<LogFolder>((Func<Task<LogFolder>>)load_logfolder);
        }
        public AsyncLazy<LogFolder> LogFolderLazyAsync {get{return _logfolder;}}
        
        /// <summary>
        /// Get the Website Configuration. Null for Websites.
        /// </summary>
        public WebsitesConfiguration WebsitesConfiguration { get; set; }
        public IEnumerable<AzureInstance> GetInstances()
        {
            throw new NotImplementedException();
        }
        public String Name { get; set; }

        public List<C1Container> Childs { get; set; }
    }

    public class WebsitesConfiguration : ConfigurationFile
    {

        public XDocument Document { get; set; }
        public WebsitesConfiguration(CloudBlockBlob blob)
            : base(blob)
        {
            Document = XDocument.Load(Stream);
        }

    }
    public static class AzureExtensions
    {
        private const string WEBSITE_CONTAINER              = "Website/";
        private const string CONFIGURATION_CONTAINER        = "Configuration/";
        private const string DIAGNOSTIC_CONTAINER           = "Diagnostics/";
        private const string INSTALL_FILES_CONTAINER        = "InstallFiles/";
        private const string UNZIPPED_WEBSITES_CONTAINER    = "UnzippedWebsites/";
        private const string ZIPPED_WEBSITES_CONTAINER      = "ZippedWebsites/";

        public static IEnumerable<C1Container> GroupByDeployments(this IEnumerable<C1Container> containers)
        {
            // Make sure that all precalculation steps have been done
            // such it do not query the storage accoutn again.
            var arr = containers.ToArray(); 
            foreach (var c in arr.Where(c => c.ContainerType == ContainerType.Deployment))
            {
                c.WebsitesConfiguration =  new WebsitesConfiguration(c.Container.GetBlockBlobReference(CONFIGURATION_CONTAINER + "Websites.xml"));
                c.Childs = new List<C1Container>();
                c.Name = c.Container.Name;
                var childblobs = c.WebsitesConfiguration.Document.Root.Elements("Website").Attributes("blobContainerName");                
                foreach (var x in childblobs.Select(a => arr.FirstOrDefault(cname => cname.Name.Equals(a.Value))))
                {
                    if(x!=null)
                        c.Childs.Add(x);
                }

                yield return c;
            }
        }

        public static CompositeContainerType CompositeContainerType(this CloudBlobContainer container)
        {
            bool[] folders = new bool[6];
            foreach (var blob in container.ListBlobs())
            {
                CloudBlobDirectory dir = blob as CloudBlobDirectory;
                if (dir==null)
                    continue;


                switch (dir.Prefix)
                {
                    case AzureExtensions.WEBSITE_CONTAINER:
                        folders[0] = true;
                        break;
                    case AzureExtensions.CONFIGURATION_CONTAINER:
                        folders[1] = true;
                        break;
                    case AzureExtensions.DIAGNOSTIC_CONTAINER:
                        folders[2] = true;
                        break;
                    case AzureExtensions.INSTALL_FILES_CONTAINER:
                        folders[3] = true;
                        break;
                    case AzureExtensions.UNZIPPED_WEBSITES_CONTAINER:
                        folders[4] = true;
                        break;
                    case AzureExtensions.ZIPPED_WEBSITES_CONTAINER:
                        folders[5] = true;
                        break;
                }



            }
               
            if (folders.Skip(1).All(x => x))
                return ContainerType.Deployment;
            else if (folders.Take(3).All(x=>x))
                return ContainerType.Website;
            else
                return ContainerType.None;
        }
    }
}
