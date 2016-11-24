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
using System.Xml.Serialization;

namespace exsim_dmb_worker
{
    public class WorkerRole : RoleEntryPoint
    {
        private CloudBlobClient BlobClient;
        private CloudBlobContainer blobContainer;
        private CloudTableClient TableClient;
        private string EventID;
        private string StationName;
        private string StationGroup;
        private CloudStorageAccount storageAccount;
        private string LocalStorageRoot;

        public override void Run()
        {
            try
            {
                CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                {
                    configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
                });

                this.storageAccount = CloudStorageAccount.FromConfigurationSetting("ConnectionString");
                this.BlobClient = this.storageAccount.CreateCloudBlobClient();
                this.LocalStorageRoot = RoleEnvironment.GetLocalResource("LocalStorage").RootPath;

                var StationsQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("stations");

                int NumberOfMessages = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("NumberOfMessages"));
     
                DownloadSimulationFiles();

                while (true)
                {
                    Thread.Sleep(10000);
                    Trace.WriteLine(System.DateTime.Now.ToLongTimeString());
                    var message = StationsQueue.GetMessages(NumberOfMessages ,TimeSpan.FromMinutes(20));
                    if (message.Count()>0 )
                    {
                        try
                        {
                            List<ManualResetEvent> doneEvents = new List<ManualResetEvent>();
                            DateTime start = System.DateTime.Now;
                            //Trace.WriteLine(message.AsString);
                           // Trace.WriteLine(String.Format("Start : {0}", System.DateTime.Now.ToLongTimeString()));
                            foreach (var item in message)
                            {
                                ManualResetEvent done = new ManualResetEvent(false);
                                doneEvents.Add(done);
                                Simulation sim = new Simulation(item , done, this.LocalStorageRoot, this.storageAccount);
                                ThreadPool.QueueUserWorkItem(sim.ThreadPoolCallback, 0);   
                            }
                          
                            WaitHandle.WaitAll(doneEvents.ToArray()); 
                            DateTime end = System.DateTime.Now;

                            Trace.WriteLine((String.Format("End : {0}", System.DateTime.Now.ToLongTimeString())));
                            
                           
                        }
                        catch (Exception)
                        {
                            //if (message.DequeueCount > 1)
                            //    StationsQueue.DeleteMessage(message);
                        }
                      
                    }
                }
            }
            catch (Exception x1)
            {
                Trace.WriteLine(x1.Message);
            }
        }

        //private void ExecuteExsim_dmb(CloudQueueMessage message)
        //{
        //    CreateInputFile(message);

        //    Process p = new Process()
        //    {
        //        StartInfo = new ProcessStartInfo(Path.Combine(this.LocalStorageRoot, @"exsim_dmb.exe"))
        //        {
        //            UseShellExecute = false,
        //            WorkingDirectory = this.LocalStorageRoot,
        //            RedirectStandardInput = true,
        //            CreateNoWindow = true
        //        }
        //    };
        //    p.Start();
        //    p.StandardInput.WriteLine("exsim_dmb.params");
        //    p.WaitForExit();
        //    p.Close();

        //}

        private void UploadResultsToBlobStorage(DateTime start, DateTime end)
        {
            var table = this.storageAccount.CreateCloudTableClient().GetDataServiceContext();

            StreamReader sr = new StreamReader(Path.Combine(this.LocalStorageRoot, "Results_distances_psa.out"));
            sr.ReadLine();
            table.AddObject("Results", new CalculationResults()
            {
                PartitionKey = this.EventID,
                RowKey = this.StationName,
                Start = start,
                End = end,
                SimulationTime = (end - start).TotalSeconds ,
                Data = sr.ReadLine()
            });
            sr.Close();
            table.SaveChanges();

            foreach (var item in new System.IO.DirectoryInfo(this.LocalStorageRoot).GetFiles("*.out", SearchOption.AllDirectories))
            {
                var blob = this.blobContainer.GetBlobReference(String.Format("{0}/{1}", StationName, item.Name));
                var file = Path.Combine(this.LocalStorageRoot, item.Name);
                if (File.Exists(file))
                {
                    blob.UploadFile(file, new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(5) });
                    Trace.WriteLine(file);
                    File.Delete(file);
                }
            }
        }

        private void ExecuteGmpe_maps(CloudQueueMessage message)
        {
            //var StationsList = from c in storageAccount.CreateCloudTableClient().GetDataServiceContext()
            //                         .CreateQuery<StationsInfo>("Stations").AsTableServiceQuery()
            //                   select c;

            //StreamWriter sw = new StreamWriter(Path.Combine(this.LocalStorageRoot, "sta_grid.txt"));
            //string Soil=String.Empty ;
            //foreach (var item in StationsList )
            //{
            //    switch (item.SoilCondition.ToLower())
            //    {
            //        case "rock":
            //            Soil="0";
            //        break;
            //        case "soil":
            //            Soil="1";
            //        break;
            //        case "soft-soil":
            //            Soil="2";
            //            break;

            //    }
            //    sw.WriteLine(string.Format("{0} {1} {2}",item.Latitude , item.Longitude,Soil));
            //}

            //sw.Close();

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

        }

        private void DownloadSimulationFiles()
        {

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            foreach (var item in blobClient.GetContainerReference("simulation").ListBlobs())
            {
                CloudBlob blob = blobClient.GetBlobReference(item.Uri.ToString());

                FileStream _file = new FileStream(Path.Combine(this.LocalStorageRoot, item.Uri.ToString().Substring(item.Uri.ToString().LastIndexOf('/') + 1)), FileMode.Create);
                Trace.WriteLine(_file.Name);
                blob.DownloadToStream(_file);
                Trace.WriteLine("Done");
                _file.Close();
            }

        }

        //protected void CreateInputFile(CloudQueueMessage message)
        //{
        //    string TextStationInput;
            
        //    //string[] messageContent = message.AsString.Split(';');
        //    this.EventID = quake.EventID;
        //    this.StationGroup = quake.StationsGroup ;
        //    this.StationName = quake.StationCode ;

        //    this.blobContainer = BlobClient.GetContainerReference(EventID);

        //    StreamReader sr = new StreamReader(Path.Combine(this.LocalStorageRoot, "init.params"));
        //    string TemplatedInput = sr.ReadToEnd();
        //    sr.Close();
        //    var station = (from c in this.storageAccount.CreateCloudTableClient().GetDataServiceContext().CreateQuery<StationsInfo>("Stations").AsTableServiceQuery()
        //                   where (c.Code == this.StationName &&c.PartitionKey == this.StationGroup) 
        //                   select c).FirstOrDefault();
        //    if (station != null)
        //    {

        //        TextStationInput = TemplatedInput.Replace("$Title", quake.EventID )
        //                                    .Replace("$Magnitute", quake.Magnitude.ToString() )
        //                                    .Replace("$Stress", quake.Stress.ToString() ) 
        //                                    .Replace("$Flag", quake.Flag  ) //hardcoded
        //                                    .Replace("$kappa", String.Format("{0}", station.KappaCoeffient))
        //                                    .Replace("$LatFault", quake.Lat.ToString() )
        //                                    .Replace("$LonFault", quake.Lon.ToString())
        //                                    .Replace("$Strike", quake.Strike.ToString()  )
        //                                    .Replace("$Dip", quake.Dip.ToString() )
        //                                    .Replace("$Depth", quake.Depth.ToString() )
        //                                    .Replace("$FaultType", quake.FaultType  )
        //                                    .Replace("$OutputFilename", "Results")
        //                                    .Replace("$CrustalAmplificationFile", station.CrustalAmplification)
        //                                    .Replace("$SiteAmplificationFile", station.SiteAmplification)
        //                                    .Replace("$SlipWeightsFile", "slip_weights.txt")
        //                                    .Replace("$LatitudeSite", String.Format("{0}", station.Latitude))
        //                                    .Replace("$LongitudeSite", String.Format("{0}", station.Longitude));


        //        StreamWriter sw = new StreamWriter(Path.Combine(this.LocalStorageRoot, "exsim_dmb.params"));
        //        sw.Write(TextStationInput);
        //        sw.Close();
        //    }
        //    return;

        //}


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
