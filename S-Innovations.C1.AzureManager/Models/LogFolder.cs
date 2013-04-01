using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace S_Innovations.C1.AzureManager.Models
{
    public class LogFolder : IDisposable
    {
        private CloudBlobDirectory _server_ref;
        private List<ConfigurationFile> Files = new List<ConfigurationFile>();
        private bool disposed = false;
        //private Thread renewalThread;

        //public delegate void LogUpdatedEventHandler(object sender, EventArgs e);
        public event EventHandler<List<String>> LogUpdated;

        public String Text { get; set; }
        //public String Text;

        public LogFolder(CloudBlobContainer DeploymentContainer, string folder)
        {
            Text = "";

            _server_ref = DeploymentContainer.GetDirectoryReference(folder);

           // Start();

        }

        private System.Timers.Timer aTimer;
        public async Task Start()
        {
            await Task.Run(() =>
            {
                Files.AddRange(_server_ref.ListBlobs().Select(b => new ConfigurationFile(b as CloudBlockBlob)));

                foreach (var f in Files)
                {
                    var t = new StreamReader(f.Stream, Encoding.UTF8);
                    Text += t.ReadToEnd();
                    //List<String> lines = new List<string>();
                    //while (!t.EndOfStream)
                   //     lines.Add(t.ReadLine());
                    //Text += String.Join("\n", lines.Reverse<String>());

                }
            });
            aTimer = new System.Timers.Timer(10000);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Enabled = true;
            
            
        //    renewalThread = new Thread(() =>
        //    {

        //        while (!disposed)
        //        {
        //            Thread.Sleep(TimeSpan.FromSeconds(10));

        //            try
        //            {

        //                if (LogUpdated != null)
        //                    update();
        //            }
        //            catch (Exception ex)
        //            {
                        
        //            }
        //        }

        //    });
        //    renewalThread.Start();
        }
        
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
             if (LogUpdated != null)
                            update();
        }
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
                    //if (renewalThread != null)
                    //{
                    //    renewalThread.Abort();

                    //    renewalThread = null;
                    //}
                    aTimer.Dispose();

                }
                disposed = true;
            }
        }
        public void update()
        {
            
            var strs = Files.Where(f => f.IsChanged()).Select(f => f.update()).SelectMany(t => t).ToList();
            if (strs.Count > 0)
            {

                Text = Text + string.Join("\n", strs) + "\n";
                if (LogUpdated != null)
                    LogUpdated(this, strs);
            }
 
        }




    }
}
