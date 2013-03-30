using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace S_Innovations.C1.AzureManager.Models
{
    public class LogFolder
    {
        CloudBlobDirectory _server_ref;
        List<ConfigurationFile> Files = new List<ConfigurationFile>();

        public String Text { get; set; }
        //public String Text;

        public LogFolder(CloudBlobContainer DeploymentContainer, string folder)
        {
            Text = "";

            _server_ref = DeploymentContainer.GetDirectoryReference(folder);
            Files.AddRange(_server_ref.ListBlobs().Select(b => new ConfigurationFile(b as CloudBlockBlob)));

            foreach (var f in Files)
            {
                var t = new StreamReader(f.Stream, Encoding.UTF8);
                List<String> lines = new List<string>();
                while (!t.EndOfStream)
                    lines.Add(t.ReadLine());
                Text += String.Join("\n", lines.Reverse<String>());

            }

        }

        public IEnumerable<String> update()
        {

            return Files.Where(f => f.IsChanged()).Select(f => f.update()).SelectMany(t => t);

        }




    }
}
