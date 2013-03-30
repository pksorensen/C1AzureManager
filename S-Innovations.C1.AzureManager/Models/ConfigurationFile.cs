using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;

namespace S_Innovations.C1.AzureManager.Models
{
    public class ConfigurationFile
    {
        private CloudBlockBlob _server_ref;

        public string ETag { get; set; }
        public long Length { get; set; }
        //public Encoding FEncoding { get; set; }
        public MemoryStream Stream { get; set; }


        public ConfigurationFile(CloudBlobContainer DeploymentContainer, string file)
        {
            //var config = DeploymentContainer.GetDirectoryReference("Configuration");
            init(DeploymentContainer.GetBlockBlobReference(file));

            //
        }
        public ConfigurationFile(CloudBlockBlob blob)
        {
            init(blob);
        }

        private void init(CloudBlockBlob blob)
        {
            if (blob == null)
                throw new ArgumentNullException("blob");

            _server_ref = blob;

            Stream = new MemoryStream();
            _server_ref.DownloadToStream(Stream);
            Stream.Seek(0, SeekOrigin.Begin);

            //Automatic Fetches Attributes on DownloadToStream.
            ETag = _server_ref.Properties.ETag;
            Length = _server_ref.Properties.Length;

        }

        public bool IsChanged()
        {
            _server_ref.FetchAttributes();
            return _server_ref.Properties.ETag != ETag;

        }
        public List<String> update()
        {

            try
            {
                Stream.Seek(0, SeekOrigin.End);
                long oldpos = Stream.Position;

                _server_ref.DownloadRangeToStream(Stream, Length, _server_ref.Properties.Length - Length);

                Stream.Seek(oldpos, SeekOrigin.Begin);
                var returnval = new List<String>();
                var sr = new StreamReader(Stream);

                while (!sr.EndOfStream)
                    returnval.Add(sr.ReadLine());



                Length = _server_ref.Properties.Length;
                ETag = _server_ref.Properties.ETag;
                return returnval;
            }
            catch (Exception e)
            {
                return new List<String>();
            }

        }
    }
}
