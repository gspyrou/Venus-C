using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;
using Model;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ReducerWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private CloudBlobClient BlobClient;
        private CloudBlobContainer blobContainer;
        private string EventID;
        private CloudStorageAccount storageAccount;
        private string LocalStorageRoot;
        public override void Run()
        {
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            {
                configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
            });

            this.storageAccount = CloudStorageAccount.FromConfigurationSetting("ConnectionString");
            this.BlobClient = this.storageAccount.CreateCloudBlobClient();
            this.LocalStorageRoot = RoleEnvironment.GetLocalResource("LocalStorage").RootPath;

            var ReducerQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("reducer");

            DownloadSimulationFiles();
            while (true)
            {
                Thread.Sleep(10000);
                CloudQueueMessage message = ReducerQueue.GetMessage();
                if (message != null)
                {

                    string[] msg = message.AsString.Split(';');
                    
                    this.blobContainer = BlobClient.GetContainerReference(msg[0].ToLower() );

                    Trace.WriteLine(String.Format("{0} Number of blobs : {1}", System.DateTime.Now.ToLongTimeString(), this.BlobClient.GetContainerReference(msg[0]).ListBlobs().Count()));
                    if (this.BlobClient.GetContainerReference(msg[0]).ListBlobs().Count() == Convert.ToInt32(msg[1]))
                    {
                        ExecuteGmtScript(message);
                        //ExecuteGmpe_maps(message);

                        ReducerQueue.DeleteMessage(message);
                        if (msg[2].ToLower()  == "true")
                            DeleteDeployment();
                    }
                }
            }
        }
        private void ExecuteGmtScript(CloudQueueMessage message)
        {
    

            string[] messageContent = message.AsString.Split(';');

            var data = from c in storageAccount.CreateCloudTableClient().GetDataServiceContext()
                                            .CreateQuery<CalculationResults>("Results").AsTableServiceQuery()
                       where c.PartitionKey == messageContent[0]
                       select c;

            StreamWriter sw = new StreamWriter(Path.Combine(this.LocalStorageRoot ,"distances.out"));
            foreach (var item in data )
            {
                sw.WriteLine(item.Data);
            }
            sw.Close();
    
            Process test = new Process()
            {
                StartInfo = new ProcessStartInfo(Path.Combine(this.LocalStorageRoot, @"test.bat"))
                {
                    UseShellExecute = true,
                    WorkingDirectory = this.LocalStorageRoot,
                    RedirectStandardInput = false,
                    CreateNoWindow = true
                }
            };
            test.Start();
            test.WaitForExit();

            foreach (var item in new System.IO.DirectoryInfo(this.LocalStorageRoot).GetFiles("*.ps", SearchOption.AllDirectories))
            {
                var blob = this.blobContainer.GetBlobReference(String.Format("{0}", item.Name));
                var file = Path.Combine(this.LocalStorageRoot, item.Name);
                if (File.Exists(file))
                {
                    blob.UploadFile(file, new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(5) });
                    Trace.WriteLine(file);
                   // File.Delete(file);
                }
            }
            //var blob = this.blobContainer.GetBlobReference(String.Format("{0}" ,"map_test.ps"));
            //var file = Path.Combine(this.LocalStorageRoot, "map_test.ps");
            //if (File.Exists(file))
            //    blob.UploadFile(file);

            var blob1 = this.blobContainer.GetBlobReference(String.Format("{0}", "distances.out"));
            var file1 = Path.Combine(this.LocalStorageRoot, "distances.out");
            if (File.Exists(file1))
            {
                blob1.UploadFile(file1);
               // File.Delete(file1);
            }
            var blob2 = this.blobContainer.GetBlobReference(String.Format("{0}","map.png"));
            var file2 = Path.Combine(this.LocalStorageRoot, "map.png");
            if (File.Exists(file2))
            {
                blob2.UploadFile(file2);
                //File.Delete(file2);
            }
            
 
        }

        private void DeleteDeployment()
        {
             var requestUri = new Uri(String.Format(@"https://management.core.windows.net/{0}/services/hostedservices/cloudquake/deploymentslots/production", "e789c94f-3b20-4884-a1a9-0a21f853e81e"));
             
            var httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(requestUri);

            httpWebRequest.Method = "DELETE";

            X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String("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"));
            // Add the certificate to the request.
            httpWebRequest.ClientCertificates.Add(certificate);

            // Specify the version information in the header.
            httpWebRequest.Headers.Add("x-ms-version", "2011-10-01");

            // Make the call using the web request.
            var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            // Display the web response status code.
            Console.WriteLine("Response status code: " + httpWebResponse.StatusCode);

            // Display the request ID returned by Windows Azure.
            if (null != httpWebResponse.Headers)
            {
                Trace.WriteLine("x-ms-request-id: "
                + httpWebResponse.Headers["x-ms-request-id"]);
            }

            // Parse the web response.
            var responseStream = httpWebResponse.GetResponseStream();
            var reader = new StreamReader(responseStream);
            // Display the raw response.
            Trace.WriteLine("Response output:");
            Trace.WriteLine(reader.ReadToEnd());

            // Close the resources no longer needed.
            httpWebResponse.Close();
            responseStream.Close();
            reader.Close();
        }
       
        private void ExecuteGmpe_maps(CloudQueueMessage message)
        {
            var StationsList = from c in storageAccount.CreateCloudTableClient().GetDataServiceContext()
                                     .CreateQuery<StationsInfo>("Stations").AsTableServiceQuery()
                               select c;

            StreamWriter sw = new StreamWriter(Path.Combine(this.LocalStorageRoot, "sta_grid.txt"));
            string Soil = String.Empty;
            foreach (var item in StationsList)
            {
                switch (item.SoilCondition.ToLower())
                {
                    case "rock":
                        Soil = "0";
                        break;
                    case "soil":
                        Soil = "1";
                        break;
                    case "soft-soil":
                        Soil = "2";
                        break;

                }
                sw.WriteLine(string.Format("{0} {1} {2}", item.Latitude, item.Longitude, Soil));
            }

            sw.Close();

            string[] messageContent = message.AsString.Split(';');

            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo(Path.Combine(this.LocalStorageRoot, @"gmpe_maps.exe"))
                {
                    UseShellExecute = false,
                    WorkingDirectory = this.LocalStorageRoot,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            p.StandardInput.WriteLine(String.Format("{0} {1} {2} {3}", messageContent[2], messageContent[3], messageContent[4], messageContent[1]));
            p.StandardInput.WriteLine("1");//Hardcoded !
            p.StandardInput.WriteLine("-1");
            p.WaitForExit();
            p.Close();

            var blob = this.blobContainer.GetBlobReference(String.Format("{0}", "pga_grid_sta.out"));
            var file = Path.Combine(this.LocalStorageRoot, "pga_grid_sta.out");
            if (File.Exists(file))
                blob.UploadFile(file);

            var blob2 = this.blobContainer.GetBlobReference(String.Format("{0}", "pgv_grid_sta.out"));
            var file2 = Path.Combine(this.LocalStorageRoot, "pgv_grid_sta.out");
            if (File.Exists(file2))
                blob2.UploadFile(file2);

        }

        private void DownloadSimulationFiles()
        {
          
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            foreach (var item in blobClient.GetContainerReference("gmt").ListBlobs())
            {
                CloudBlob blob = blobClient.GetBlobReference(item.Uri.ToString());

                FileStream _file = new FileStream(Path.Combine(this.LocalStorageRoot, item.Uri.ToString().Substring(item.Uri.ToString().LastIndexOf('/') + 1)), FileMode.Create);

                blob.DownloadToStream(_file, new BlobRequestOptions() { Timeout = System.TimeSpan.FromMinutes(5) });
                _file.Close();

            }

        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
