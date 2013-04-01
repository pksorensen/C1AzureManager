using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using CompositeC1AzureDynamicWebRole;
using Microsoft.Web.Administration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.IO.Pipes;



namespace WebRole
{
    public class WebRole : RoleEntryPoint
    {
        private const string DeploymentNameConfigName = "DeploymentName";
        private const string DefaultWebsiteNameConfigName = "DefaultWebsiteName";
        private const string RegionNameConfigName = "DisplayName";
        private const string CompoisteC1AzureRuntimeFilesUrlConfigName = "CompositeC1AzureRuntimeFilesUrl";
        private const string BlobConnectionStringConfigName = "BlobConnectionString";
        private const string ZippedWebsiteUrlConfigName = "ZippedWebsiteUrl";
        private const string CheckConfigUpdateTimeConfigName = "CheckConfigUpdateTime";
        private const string ApplicationPoolIdleTimeoutConfigName = "ApplicationPoolIdleTimeout";
        private const string DeleteWebsiteBlobWhenRemovingWebsiteConfigName = "DeleteWebsiteBlobWhenRemovingWebsite";

        private const string InstallFilesBlobDirectoryName = "InstallFiles";
        private const string LogFilesBlobDirectory = "Diagnostics/WebRoleLogs";

        private Logger _logger;

        private volatile bool _keepRunning = true;
        private volatile bool _newDynamicWebRoleReady = false;

        private string _configurationContainerName;
        private DynamicWebRoleHandler _dynamicWebRoleHandler;


        public override bool OnStart()
        {
            try
            {
                string deploymentName = RoleEnvironment.GetConfigurationSettingValue(DeploymentNameConfigName);
                string compoisteC1AzureRuntimeFilesUrl = RoleEnvironment.GetConfigurationSettingValue(CompoisteC1AzureRuntimeFilesUrlConfigName);
                string blobConnectionString = RoleEnvironment.GetConfigurationSettingValue(BlobConnectionStringConfigName);

                _configurationContainerName = deploymentName.ToLower();

                string loggerPrefix = RoleEnvironment.DeploymentId;
                if (!string.IsNullOrEmpty(RoleEnvironment.GetConfigurationSettingValue(RegionNameConfigName)))
                {
                    loggerPrefix += "·" + RoleEnvironment.GetConfigurationSettingValue(RegionNameConfigName);
                }
                else
                {
                    loggerPrefix += "·";
                }

                loggerPrefix += "·" + RoleEnvironment.CurrentRoleInstance.Id + "·" + "Log";

                _logger = new Logger(loggerPrefix);
                _logger.InitializeBlob(blobConnectionString, _configurationContainerName, LogFilesBlobDirectory);

                _logger.Add("DeploymentId: " + RoleEnvironment.DeploymentId);
                _logger.Add("AppDomainId: " + AppDomain.CurrentDomain.Id);
                _logger.Add("ThreadId: " + Thread.CurrentThread.ManagedThreadId);

                _logger.Add("Configuration");
                _logger.Add(CreateConfigLogString("DeploymentName", DeploymentNameConfigName));
                _logger.Add(CreateConfigLogString("DefaultWebsiteName", DefaultWebsiteNameConfigName));
                _logger.Add(CreateConfigLogString("DisplayName", RegionNameConfigName));
                _logger.Add(CreateConfigLogString("ZippedWebsiteUrl", ZippedWebsiteUrlConfigName));
                _logger.Add(CreateConfigLogString("CompoisteC1AzureRuntimeFilesUrl", CompoisteC1AzureRuntimeFilesUrlConfigName));
                _logger.Add(CreateConfigLogString("CheckConfigUpdateTime", CheckConfigUpdateTimeConfigName));
                _logger.Add(CreateConfigLogString("ApplicationPoolIdleTimeout", ApplicationPoolIdleTimeoutConfigName));
                _logger.Add(CreateConfigLogString("DeleteWebsiteBlobWhenRemovingWebsite", DeleteWebsiteBlobWhenRemovingWebsiteConfigName));


#warning MRJ: Move this code to the logger?
                new Thread(() =>
                {
#warning MRJ: Find a way to kill this loop and have WaitForConnection time out
                    while (true)
                    {
                        NamedPipeServerStream pipeServerStream = new NamedPipeServerStream("CompositeC1AzurePipe", PipeDirection.In);

                        pipeServerStream.WaitForConnection();

                        using (StreamReader streamReader = new StreamReader(pipeServerStream))
                        {
                            string entry = streamReader.ReadToEnd();
                            _logger.Add(entry);
                        }
                    }
                }).Start();

                _dynamicWebRoleHandler = new DynamicWebRoleHandler(
                        "CompositeC1AzureRuntime.WebRoleHandler",
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DynamicWebroles"),
                        blobConnectionString,
                        _configurationContainerName + "/" + InstallFilesBlobDirectoryName + "/" + "CompositeC1AzureRuntime.dll",
                        compoisteC1AzureRuntimeFilesUrl + "/" + "CompositeC1AzureRuntime.dll"
                    );

                _dynamicWebRoleHandler.Logger = _logger.Add;

                _dynamicWebRoleHandler.OnNewAppDomain = newHandler =>
                {
                    try
                    {
                        newHandler.OnStart();
                        _newDynamicWebRoleReady = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Add(ex);
                    }
                };

                _dynamicWebRoleHandler.OnAppDomainStopping = handler =>
                {
                    try
                    {
                        _newDynamicWebRoleReady = false;
                        handler.OnStop();
                    }
                    catch (Exception ex)
                    {
                        _logger.Add(ex);
                    }
                };

                _dynamicWebRoleHandler.StartUpdatingAppDomain();              
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.Add(ex);

                    CreateErrorPage(ex);
                }
                catch (Exception exception)
                {
                    _logger.Add(exception);
                }
            }

            return base.OnStart();
        }



        public override void Run()
        {
            try
            {
                while (_keepRunning)
                {                    
                    while (!_newDynamicWebRoleReady)
                    {
                        Thread.Sleep(1000);
                    }

                    _dynamicWebRoleHandler.CurrentDynamicWebRole.OnRun();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.Add(ex);

                    CreateErrorPage(ex);
                }
                catch (Exception exception)
                {
                    _logger.Add(exception);
                }
            }

            base.Run();
        }



        public override void OnStop()
        {
            try
            {
                _keepRunning = false;
                _newDynamicWebRoleReady = false;

                if (_dynamicWebRoleHandler.CurrentDynamicWebRole != null)
                {
                    _dynamicWebRoleHandler.CurrentDynamicWebRole.OnStop();
                }

                _dynamicWebRoleHandler.StopUpdatingAppDomain();

                _logger.ForceCopyToBlob();
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.Add(ex);

                    CreateErrorPage(ex);
                }
                catch (Exception exception)
                {
                    _logger.Add(exception);
                }
            }

            base.OnStop();
        }


        #region Logging

        private void CreateErrorPage(Exception ex)
        {
            foreach (string websitePath in WebsitePaths.AllWebsiteRootPaths)
            {
                try
                {

                    string path = Path.Combine(websitePath, "Default.aspx");

                    _logger.Add("Creating error page (" + path + ")");

                    string pageContent = @"<html><head><title>Error deploying website</title><head><body>";

                    pageContent += "<b>Exception</b><br />";
                    while (ex != null)
                    {
                        pageContent += "Message: <span style='color: red'>" + ex.Message + "</span><br />";
                        pageContent += "Stack: " + ex.StackTrace + "<br />";
                        pageContent += "<br />";

                        ex = ex.InnerException;
                    }
                    pageContent += "<br />";


                    pageContent += "<b>Configuration</b><br />";
                    pageContent += CreateConfigLogString("DeploymentName", DeploymentNameConfigName) + "<br />";
                    pageContent += CreateConfigLogString("ZippedWebsiteUrl", ZippedWebsiteUrlConfigName) + "<br />";
                    pageContent += CreateConfigLogString("CompoisteC1AzureRuntimeFilesUrl", CompoisteC1AzureRuntimeFilesUrlConfigName) + "<br />";
                    pageContent += "<br />";

                    pageContent += "<b>Log</b><br />";
                    foreach (string line in File.ReadAllLines(_logger.LocalLogFilePath))
                    {
                        pageContent += line + "<br />";
                    }
                    pageContent += "<br />";

                    pageContent += "</body>";

                    File.WriteAllText(path, pageContent);
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }



        private static string CreateConfigLogString(string title, string configName)
        {
            try
            {
                return title + ": " + RoleEnvironment.GetConfigurationSettingValue(configName) ?? "MISSING!";
            }
            catch (Exception ex)
            {
                return title + ": " + "Failed to get configuration value with nane '" + configName + "'" + " with exception: " + ex.Message;
            }
        }

        #endregion
    }







    /// <summary>
    /// This class is not supposed to be used directly.
    /// You should copy/past this into your WebRole.cs file and
    /// use it there.
    /// 
    /// Version 1.0 (Please update version if this file is changed!)
    /// </summary>
    public class Logger
    {
        private const int MaxLogFileSize = 5 * 1024 * 1024;
#warning MRJ: Make this larger!
        private const int UploadLogFileIntervall = 5000; // 5 * 60 * 1000;

        private readonly string _logFileNamePrefix;
        private readonly object _lock = new object();


        private CloudBlobContainer _blobContainer;
        private string _blobDirectoryName;
        private bool _blobReady = false;
        private int _blobLogCounter = 0;
        private bool _logFileIsDirty = false;
        private Thread _copyLogToBlobThread;
        private bool _stopCopyingLogToBlob = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logFileNamePrefix">
        /// The prefix of the file name of the log. 
        /// This is used both locally and for the naming the blob.
        /// </param>
        public Logger(string logFileNamePrefix)
        {
            _logFileNamePrefix = logFileNamePrefix;
        }



        /// <summary>
        /// Initializes copying of the log file to the blob periodicly.
        /// </summary>
        /// <param name="blobConnectionString"></param>
        /// <param name="containerName"></param>
        /// <param name="blobDirectoryName"></param>
        public void InitializeBlob(string blobConnectionString, string containerName, string blobDirectoryName, bool backupOldFiles = false)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobConnectionString);

                CloudBlobClient client = storageAccount.CreateCloudBlobClient();

                _blobContainer = client.GetContainerReference(containerName);
                _blobContainer.CreateIfNotExist();

                _blobDirectoryName = blobDirectoryName;

                if (backupOldFiles)
                {
                    BackupOldLogFiles(blobDirectoryName);
                }

                _blobReady = true;

                _copyLogToBlobThread = new Thread(CopyLogToBlobHandler);
                _copyLogToBlobThread.Start();

                Add("Logger initialized with blob backup in: " + _blobContainer.Uri + "/" + blobDirectoryName);
            }
            catch (Exception ex)
            {
                Add(ex);
            }
        }



        /// <summary>
        /// Stops the copying of the log to the blob.
        /// </summary>
        /// <param name="waitForThreadToStop">If this is true this method will block until the copy-to-blob thread is done.</param>
        public void FinalizeBlob(bool waitForThreadToStop = false)
        {
            _stopCopyingLogToBlob = true;

            if (waitForThreadToStop && _copyLogToBlobThread != null)
            {
                _copyLogToBlobThread.Join();
            }
        }



        public void ForceCopyToBlob()
        {
            if (!_blobReady) return;

            CopyLogToBlob();
        }



        private void CopyLogToBlobHandler()
        {
            while (!_stopCopyingLogToBlob)
            {
                Thread.Sleep(UploadLogFileIntervall);

                if (_logFileIsDirty)
                {
                    CopyLogToBlob();
                }
            }
        }



        private void CopyLogToBlob()
        {
            try
            {
                lock (_lock)
                {
                    if (!File.Exists(LocalLogFilePath)) return;
                    FileInfo fileInfo = new FileInfo(LocalLogFilePath);
                    if (fileInfo.Length == 0) return;

                    CloudBlob logBlob = _blobContainer.GetBlobReference(CurrentLogBlobFileName);

                    logBlob.UploadFile(LocalLogFilePath);

                    _logFileIsDirty = false;
                }
            }
            catch (Exception ex)
            {
                Add(ex);
            }
        }



        private void BackupOldLogFiles(string folderName)
        {
            CloudBlobDirectory cloudBlobDirectory = _blobContainer.GetDirectoryReference(folderName);

            List<CloudBlob> oldLogFiles = cloudBlobDirectory.ListBlobs().OfType<CloudBlob>().ToList();

            if (oldLogFiles.Count > 0)
            {
                string oldBlobNameDirectory = "OldLogFiles-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                foreach (CloudBlob blob in oldLogFiles)
                {
                    string blobFileName = blob.Uri.LocalPath;
                    blobFileName = blobFileName.Substring(blobFileName.LastIndexOf("/") + 1);

                    string newBlobName = folderName + "/" + oldBlobNameDirectory + "/" + blobFileName;

                    CloudBlob newBlob = _blobContainer.GetBlobReference(newBlobName);

                    try
                    {
                        newBlob.CopyFromBlob(blob);

                        blob.Delete();
                    }
                    catch (Exception ex)
                    {
                        // Log exception
                    }
                }
            }
        }



        private string CurrentLogBlobFileName
        {
            get
            {
                return _blobDirectoryName + "/" + _logFileNamePrefix + _blobLogCounter.ToString("0000") + ".txt";
            }
        }


        /// <summary>
        /// Adds log entires for the given exception.
        /// </summary>
        /// <param name="ex"></param>
        public void Add(Exception ex)
        {
            while (ex != null)
            {
                Add("Exception:");
                Add("Message: " + ex.Message);
                Add("Stack: " + ex.StackTrace);

                ex = ex.InnerException;
            }
        }



        /// <summary>
        /// Adds a log entry to the log.
        /// </summary>
        /// <param name="entry"></param>
        public void Add(string entry)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.Append(" : ");
                sb.AppendLine(entry);

                lock (_lock)
                {
                    File.AppendAllText(LocalLogFilePath, sb.ToString());
                    _logFileIsDirty = true;

                    FileInfo fileInfo = new FileInfo(LocalLogFilePath);
                    if (fileInfo.Length > MaxLogFileSize)
                    {
                        ForceCopyToBlob();
                        _blobLogCounter++;
                        File.Copy(LocalLogFilePath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LocalLogFileName + _blobLogCounter));
                        File.WriteAllText(LocalLogFilePath, "");
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }



        /// <summary>
        /// The local file path of the log.
        /// </summary>
        public string LocalLogFilePath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LocalLogFileName);
            }
        }



        private string LocalLogFileName
        {
            get
            {
                return _logFileNamePrefix + ".txt";
            }
        }
    }



    /// <summary>
    /// This class is not supposed to be used directly.
    /// You should copy/past this into your WebRole.cs file and
    /// use it there.
    /// 
    /// Version 1.0 (Please update version if this file is changed!)
    /// </summary>
    public class DynamicWebRoleHandler
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudBlobClient _client;
        private readonly string _dynamicWebRoleHandlerTypeFullName;
        private readonly string _workfolder;
        private readonly string _blobPath;
        private readonly string _fallbackDownloadUrl;
        private volatile bool _keepRunning;
        private int _localNameCounter = 0;
        private DateTime _lastModifiedUtc = DateTime.MinValue;
        private AppDomain _appDomain = null;
        private IDynamicWebRole _dynamicWebRole;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dynamicWebRoleHandlerTypeFullName">The full name of the dynamic webrole handler type.</param>
        /// <param name="workfolder">This folder is used for downloading the console assembly and as base directory for new app domains.</param>
        /// <param name="blobConnectionString">Connection string to the blob where the console assembly is located and updated.</param>
        /// <param name="blobPath">Path in the blob to the console assembly.</param>
        /// <param name="fallbackDownloadUrl">URL to get the default console assembly in case there is no one in the blob.</param>
        public DynamicWebRoleHandler(string dynamicWebRoleHandlerTypeFullName, string workfolder, string blobConnectionString, string blobPath, string fallbackDownloadUrl)
        {
            _storageAccount = CloudStorageAccount.Parse(blobConnectionString);
            _client = _storageAccount.CreateCloudBlobClient();

            _dynamicWebRoleHandlerTypeFullName = dynamicWebRoleHandlerTypeFullName;
            _workfolder = workfolder;
            _blobPath = blobPath;
            _fallbackDownloadUrl = fallbackDownloadUrl;
        }



        public Action<string> Logger { get; set; }


        public IDynamicWebRole CurrentDynamicWebRole
        {
            get { return _dynamicWebRole; }
        }



        public Action<IDynamicWebRole> OnNewAppDomain { get; set; }
        public Action<IDynamicWebRole> OnAppDomainStopping { get; set; }


        /// <summary>
        /// This methods starts a new thread that will periodicly check to see if there is a new version of the assemlby. 
        /// If there is, the current running app domain will be stoped and a new one started with the new assembly.
        /// </summary>
        public void StartUpdatingAppDomain(bool blockUntilRoleIsRunning = true)
        {
            AddLogEntry("DynamicWebRoleHandler started using workfolder: " + _workfolder + " blob path: " + _client.GetBlobReference(_blobPath).Uri + " fallback download url: " + _fallbackDownloadUrl);

            _keepRunning = true;
            new Thread(() =>
            {
                while (_keepRunning)
                {
                    try
                    {
                        DateTime lastModified = GetTimeStamp();
                        if (lastModified > _lastModifiedUtc)
                        {
                            AddLogEntry("Dynamic bits are out of date");

                            string fileName = DownloadToLocal();

                            UpdateAppDomain(fileName);

                            _lastModifiedUtc = lastModified;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry(ex.ToString());
                    }

                    Thread.Sleep(30 * 1000);
                }

                StopAppDomain();
            }).Start();

            if (blockUntilRoleIsRunning)
            {
                while (_dynamicWebRole == null)
                {
                    Thread.Sleep(10);
                }
            }
        }



        /// <summary>
        /// Stops the auto updating of the app domain and stops the current app domain.
        /// </summary>
        public void StopUpdatingAppDomain()
        {
            _keepRunning = false;
        }



        private void UpdateAppDomain(string assemblyFileName)
        {
            if (_appDomain != null)
            {
                StopAppDomain();
            }

            AddLogEntry("Creating new AppDomain using new dynamic bits");

            AppDomainSetup domainSetup = new AppDomainSetup();
            domainSetup.PrivateBinPath = CurrentLocalFolder;
            domainSetup.ApplicationBase = CurrentLocalFolder;

            _appDomain = AppDomain.CreateDomain(assemblyFileName, null, domainSetup);

            _dynamicWebRole = (IDynamicWebRole)_appDomain.CreateInstanceAndUnwrap(assemblyFileName, _dynamicWebRoleHandlerTypeFullName);

            AddLogEntry("AppDomain created (" + _appDomain.Id + ")");

            if (OnNewAppDomain != null)
            {
                try
                {
                    OnNewAppDomain(_dynamicWebRole);
                }
                catch (Exception ex)
                {
                    AddLogEntry("OnNewAppDomain FAILED!");
                    AddLogEntry(ex.ToString());
                }
            }
            else
            {
                AddLogEntry("WARNING: No event handler added for OnNewAppDomain!");
            }
        }



        private void StopAppDomain()
        {
            if (_dynamicWebRole != null && OnAppDomainStopping != null)
            {
                try
                {
                    OnAppDomainStopping(_dynamicWebRole);
                }
                catch (Exception ex)
                {
                    AddLogEntry("OnAppDomainStopping FAILED!");
                    AddLogEntry(ex.ToString());
                }
            }

            if (_appDomain == null) return;

            AddLogEntry("Unloading current AppDomain (" + _appDomain.Id + ")");
            try
            {
                AppDomain.Unload(_appDomain);
            }
            catch (Exception)
            {
            }
            finally
            {
                _appDomain = null;
            }
        }




        private string DownloadToLocal()
        {
            _localNameCounter++;

            string localFolder = CurrentLocalFolder;

            if (!Directory.Exists(localFolder))
            {
                Directory.CreateDirectory(localFolder);
            }

            string localPath = Path.Combine(localFolder, _blobPath.Remove(0, _blobPath.LastIndexOf('/') + 1));

            CloudBlob blob = _client.GetBlobReference(_blobPath);
            blob.DownloadToFile(localPath);

            AddLogEntry("New dynamic bits downloaded to " + localPath);

#warning MRJ: Assuming that the CompositeC1AzureDynamicWebRole.dll is a part of the deployment
            File.Copy("CompositeC1AzureDynamicWebRole.dll", Path.Combine(localFolder, "CompositeC1AzureDynamicWebRole.dll"), true);
            File.Copy("ICSharpCode.SharpZipLib.dll", Path.Combine(localFolder, "ICSharpCode.SharpZipLib.dll"), true);
            File.Copy("Microsoft.WindowsAzure.StorageClient.dll", Path.Combine(localFolder, "Microsoft.WindowsAzure.StorageClient.dll"), true);
            File.Copy("System.Management.Automation.dll", Path.Combine(localFolder, "System.Management.Automation.dll"), true);
            File.Copy("Microsoft.Web.Administration.dll", Path.Combine(localFolder, "Microsoft.Web.Administration.dll"), true);  

            return Path.GetFileNameWithoutExtension(localPath);
        }



        private DateTime GetTimeStamp()
        {
            DateTime lastModified;

            CloudBlob blob = _client.GetBlobReference(_blobPath);
            try
            {
                blob.FetchAttributes();
                lastModified = blob.Properties.LastModifiedUtc;
            }
            catch (Exception)
            {
                if (_dynamicWebRole == null)
                {
                    AddLogEntry("No dynamic bits exists in the blob: " + blob.Uri);
                    lastModified = DownloadFromFallbackUrl();
                }
                else
                {
                    AddLogEntry("No dynamic bits exists in the blob, but dynamic domain is running. Skipping update : " + blob.Uri);
                    lastModified = _lastModifiedUtc;
                }
            }

            return lastModified;
        }



        private DateTime DownloadFromFallbackUrl()
        {
            CloudBlob blob = _client.GetBlobReference(_blobPath);
            blob.Container.CreateIfNotExist();            

            AddLogEntry("Downloading fallback dynamic bits from " + _fallbackDownloadUrl + " to " + blob.Uri);

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(_fallbackDownloadUrl);

            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();            

            using (BlobStream blobStream = blob.OpenWrite())
            using (Stream dataStream = httpResponse.GetResponseStream())
            {
                const int bufferSize = 8 * 1024;
                byte[] buffer = new byte[bufferSize];
                while (true)
                {
                    int read = dataStream.Read(buffer, 0, bufferSize);
                    if (read == 0) break;

                    blobStream.Write(buffer, 0, read);
                }
            }

            blob.FetchAttributes();
            return blob.Properties.LastModifiedUtc;
        }



        private string CurrentLocalFolder
        {
            get
            {
                return Path.Combine(_workfolder, _localNameCounter.ToString());
            }
        }



        private void AddLogEntry(string entry)
        {
            if (Logger == null) return;

            Logger(entry);
        }
    }



    /// <summary>
    /// This class is not supposed to be used directly.
    /// You should copy/past this into your WebRole.cs file and
    /// use it there.
    /// 
    /// Version 1.3 (Please update version if this file is changed!)
    /// </summary>
    public class AssemblyResolver : IDisposable
    {
        private readonly List<Tuple<string, bool>> _alloedAssemblies = new List<Tuple<string, bool>>();
        private string _baseDownloadUrl;
        private readonly CloudBlobContainer _blobContainer;
        private readonly string _blobFolderName;



        public Action<string> Logger { get; set; }


        /// <summary>
        /// Creates an instance without using the blob storage for saving
        /// assemblies
        /// </summary>
        /// <param name="baseDownloadUrl">Ex: www.mysite.com/AzureDeployFiles</param>        
        public AssemblyResolver(string baseDownloadUrl)
        {
            Initalize(baseDownloadUrl);
        }



        /// <summary>
        /// Creates and instance that will use a given assembly from a blob container
        /// if it exists. If it does not exists it will download it and save it to 
        /// the blob container for later use. 
        /// </summary>
        /// <param name="baseDownloadUrl">Ex: www.mysite.com/AzureDeployFiles</param>
        /// <param name="blobConnectionString">Connection string to the blob</param>
        /// <param name="containerName">Name of container that contains the folder that is used for storing assemblies</param>
        /// <param name="folderName">Name of the folder used for storing assemblies</param>
        public AssemblyResolver(string baseDownloadUrl, string blobConnectionString, string containerName, string folderName)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobConnectionString);

                CloudBlobClient client = storageAccount.CreateCloudBlobClient();

                _blobContainer = client.GetContainerReference(containerName);
                _blobContainer.CreateIfNotExist();
            }
            catch (Exception ex)
            {
                AddLogEntry(ex.ToString());
            }

            _blobFolderName = folderName;

            Initalize(baseDownloadUrl);
        }



        /// <summary>
        /// Creates and instance that will use a given assembly from a blob container
        /// if it exists. If it does not exists it will download it and save it to 
        /// the blob container for later use. 
        /// </summary>
        /// <param name="baseDownloadUrl">Ex: www.mysite.com/AzureDeployFiles</param>
        /// <param name="container">Container to use for storing assemblies</param>
        public AssemblyResolver(string baseDownloadUrl, CloudBlobContainer container)
        {
            _blobContainer = container;
            _blobContainer.CreateIfNotExist();

            Initalize(baseDownloadUrl);
        }



        /// <summary>
        /// Adds a name of an assembly that should be downloaded and loaded.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly not including .dll</param>
        /// <param name="allowBlobStorage">If this is true the assembly will be fetched/saved from the blob storage</param>
        public void AddAllowedAssemblyName(string assemblyName, bool allowBlobStorage = true)
        {
            _alloedAssemblies.Add(new Tuple<string, bool>(assemblyName, allowBlobStorage));
        }



        /// <summary>
        /// Resolves allowed assemblies.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string fullName = args.Name;

                Tuple<string, bool> assembly = _alloedAssemblies.Where(f => fullName.StartsWith(f.Item1, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
                if (assembly == null) return null;

                string assemblyFileName = fullName;
                if (assemblyFileName.Contains(",")) assemblyFileName = assemblyFileName.Substring(0, assemblyFileName.IndexOf(','));
                if (!assemblyFileName.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)) assemblyFileName += ".dll";

                byte[] assemblyBytes = GetAssemblyBytes(assemblyFileName, assembly.Item2);

                Assembly loadedAssembly = Assembly.Load(assemblyBytes);

                AddLogEntry("Assembly loaded: " + loadedAssembly.FullName);

                return loadedAssembly;
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    AddLogEntry("Exception:");
                    AddLogEntry("Message: " + ex.Message);
                    AddLogEntry("Stack: " + ex.StackTrace);

                    ex = ex.InnerException;
                }
            }

            return null;
        }



        /// <summary>
        /// Gets the bytes of the given assembly.
        /// </summary>
        /// <param name="assemblyFileName"></param>
        /// <param name="allowBlobStorage"></param>
        /// <returns></returns>
        private byte[] GetAssemblyBytes(string assemblyFileName, bool allowBlobStorage)
        {
            byte[] assemblyBytes;

            if (allowBlobStorage && _blobContainer != null)
            {
                if (TryGetAssemblyBytesFromBlob(assemblyFileName, out assemblyBytes))
                {
                    return assemblyBytes;
                }
            }

            assemblyBytes = DownloadAssemblyBytes(assemblyFileName, allowBlobStorage);

            return assemblyBytes;
        }




        /// <summary>
        /// Tries to get the assembly from the blob storage, if it exists.
        /// </summary>
        /// <param name="assemblyFileName"></param>
        /// <param name="assemblyBytes"></param>
        /// <returns></returns>
        private bool TryGetAssemblyBytesFromBlob(string assemblyFileName, out byte[] assemblyBytes)
        {
            try
            {
                string blobName = CreateBlobName(assemblyFileName);

                CloudBlob blob = _blobContainer.GetBlobReference(blobName);
                assemblyBytes = blob.DownloadByteArray();

                AddLogEntry("Using assembly version from: " + blob.Uri);

                return true;
            }
            catch (Exception)
            {
                // Ignore
            }

            assemblyBytes = null;

            return false;
        }




        /// <summary>
        /// Downloads the assembly from the web and saves it to the blob storage.
        /// </summary>
        /// <param name="assemblyFileName"></param>
        /// <param name="allowBlobStorage"></param>
        /// <returns></returns>
        private byte[] DownloadAssemblyBytes(string assemblyFileName, bool allowBlobStorage)
        {
            string url = _baseDownloadUrl + assemblyFileName;

            byte[] assemblyBytes = DownloadFile(url);

            if (allowBlobStorage)
            {
                try
                {
                    string blobName = CreateBlobName(assemblyFileName);

                    CloudBlob blob = _blobContainer.GetBlobReference(blobName);
                    blob.UploadByteArray(assemblyBytes);

                    AddLogEntry("Downloaded assembly saved to: " + blob.Uri);
                }
                catch (Exception ex)
                {
                    AddLogEntry(ex.ToString());
                }
            }

            return assemblyBytes;
        }




        /// <summary>
        /// Downloads a given url into a byte array.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private byte[] DownloadFile(string url)
        {
            AddLogEntry("Downloading file from: " + url);

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);

            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();

            MemoryStream memoryStream = new MemoryStream();

            using (Stream dataStream = httpResponse.GetResponseStream())
            {
                if (dataStream == null) return null;

                const int bufferSize = 8 * 1024;
                byte[] buffer = new byte[bufferSize];
                while (true)
                {
                    int read = dataStream.Read(buffer, 0, bufferSize);
                    if (read == 0) break;

                    memoryStream.Write(buffer, 0, read);
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream.ToArray();
        }



        private string CreateBlobName(string assemblyFileName)
        {
            string blobName = assemblyFileName;
            if (!string.IsNullOrEmpty(_blobFolderName)) blobName = _blobFolderName + "/" + blobName;

            return blobName;
        }



        private void Initalize(string baseDownloadUrl)
        {
            if (!baseDownloadUrl.EndsWith("/"))
            {
                baseDownloadUrl += "/";
            }

            _baseDownloadUrl = baseDownloadUrl;

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }



        private void DeInitalize()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
        }



        private void AddLogEntry(string entry)
        {
            if (Logger == null) return;

            Logger(entry);
        }



        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                DeInitalize();
                _disposed = true;
            }
        }


        ~AssemblyResolver()
        {
            Dispose(false);
        }
    }




    /// <summary>
    /// Find all websites paths within the webrole by using the 
    /// IIS ServerManager class.
    /// 
    /// This class is not supposed to be used directly.
    /// You should copy/past this into your WebRole.cs file and
    /// use it there.
    /// 
    /// Version 1.0 (Please update version if this file is changed!)
    /// </summary>
    public static class WebsitePaths
    {
        /// <summary>
        /// Returns the paths of all websites
        /// </summary>
        public static IEnumerable<string> AllWebsitePaths
        {
            get
            {
                return GetPaths(false);
            }
        }



        /// <summary>
        /// Return the paths of all website roots
        /// </summary>
        public static IEnumerable<string> AllWebsiteRootPaths
        {
            get
            {
                return GetPaths(true);
            }
        }



        private static IEnumerable<string> GetPaths(bool rootsOnly)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                foreach (Site site in serverManager.Sites)
                {
                    foreach (Application application in site.Applications)
                    {
                        foreach (VirtualDirectory virtualDirectory in application.VirtualDirectories)
                        {
                            if (rootsOnly && virtualDirectory.Path == "/") yield return virtualDirectory.PhysicalPath;
                            else if (!rootsOnly) yield return virtualDirectory.PhysicalPath;
                        }
                    }
                }
            }
        }
    }
}
