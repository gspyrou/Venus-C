using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using System.Diagnostics;
using System.IO;
using Microsoft.WindowsAzure;
using System.Xml.Serialization;
namespace exsim_dmb_worker
{
    public class Simulation
    {
        public QuakeMessage  Quake { get; set; }
        public CloudQueueMessage  Message { get; set; }
        public ManualResetEvent IsDone { get; set; }
        public string Root { get; set; }
        public CloudStorageAccount StorageAccount { get; set; }
        public CloudQueue  Queue { get; set; }
        private CloudBlobContainer Container;
        public Simulation(CloudQueueMessage message, ManualResetEvent done, string root, CloudStorageAccount account)
        {
            this.Message = message;
            this.Quake = DeserialeMessage(message);           
            this.IsDone = done;
            this.Root = root;
            this.StorageAccount = account;
            this.Queue = account.CreateCloudQueueClient().GetQueueReference("stations");
            this.Container = this.StorageAccount.CreateCloudBlobClient().GetContainerReference(this.Quake.EventID.ToLower() );    
        }
        public QuakeMessage DeserialeMessage(CloudQueueMessage msg)
        {
            Stream stream = new MemoryStream();

            StreamWriter writer = new StreamWriter(stream);
            writer.Write(msg.AsString);
            writer.Flush();

            stream.Position = 0;
            object result = new XmlSerializer(typeof(QuakeMessage)).Deserialize(stream);
            stream.Close();
            stream.Dispose();
            return (QuakeMessage)result;

        }
        public void ThreadPoolCallback(Object threadContext)
        {
            try
            {
                int threadIndex = (int)threadContext;
                DateTime start = System.DateTime.Now;
                PerformCaluclations();
                DateTime end = System.DateTime.Now;
                UploadResultsToBlobStorage(start, end);
                //Upload files and clean up!
                this.Queue.DeleteMessage(this.Message);
            
            }
            catch (Exception)
            {
                if (this.Message.DequeueCount > 1)
                    this.Queue.DeleteMessage(this.Message);
            }
           
            IsDone.Set();
        }
       
        public void PerformCaluclations()
        {
            CreateInputFile();

            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo(Path.Combine(this.Root, @"exsim_dmb.exe"))
                {
                    UseShellExecute = false,
                    WorkingDirectory = this.Root,  
                    RedirectStandardInput = true,
                    CreateNoWindow = true 
                }
            };
            p.Start();
            p.StandardInput.WriteLine(String.Format(@"{0}_{1}.params", this.Quake.EventID, this.Quake.Station.Code  ));
           
            p.WaitForExit();
            p.Close();

        }
        protected void CreateInputFile()
        {
            string TextStationInput;
            StreamReader sr = new StreamReader(Path.Combine(this.Root, "init.params"));
            string TemplatedInput = sr.ReadToEnd();
            sr.Close();
            var station = this.Quake.Station;
            if (station != null)
            {

                TextStationInput = TemplatedInput.Replace("$Title", this.Quake.EventID)
                                            .Replace("$Magnitute", this.Quake.Magnitude.ToString())
                                            .Replace("$Stress", this.Quake.Stress.ToString())
                                            .Replace("$Flag", this.Quake.Flag) //hardcoded
                                            .Replace("$kappa", String.Format("{0}", station.KappaCoeffient))
                                            .Replace("$LatFault", this.Quake.LatCalc.ToString())
                                            .Replace("$LonFault", this.Quake.LonCalc.ToString())
                                            .Replace("$Length",this.Quake.Length.ToString())
                                            .Replace("$Width",this.Quake.Width.ToString())
                                            .Replace("$Strike", this.Quake.Strike.ToString())
                                            .Replace("$Dip", this.Quake.Dip.ToString())
                                            .Replace("$Depth", this.Quake.Depth.ToString())
                                            .Replace("$FaultType", this.Quake.FaultType)
                                            .Replace("$OutputFilename", String.Format(@"{0}_{1}_", this.Quake.EventID, this.Quake.StationCode))
                                            .Replace("$CrustalAmplificationFile", station.CrustalAmplification)
                                            .Replace("$SiteAmplificationFile", station.SiteAmplification)
                                            .Replace("$SlipWeightsFile", "slip_weights.txt")
                                            .Replace("$LatitudeSite", String.Format("{0}", station.Latitude))
                                            .Replace("$LongitudeSite", String.Format("{0}", station.Longitude));


                System.IO.FileInfo file = new System.IO.FileInfo(Path.Combine(this.Root, String.Format(@"{0}_{1}.params", this.Quake.EventID, this.Quake.StationCode)));
                file.Directory.Create(); // If the directory already exists, this method does nothing. 
                System.IO.File.WriteAllText(file.FullName, TextStationInput); 

               
            }
            return;

        }
        private void UploadResultsToBlobStorage(DateTime start , DateTime end)
        {
            var table = this.StorageAccount.CreateCloudTableClient().GetDataServiceContext();

            StreamReader sr = new StreamReader(Path.Combine(this.Root, String.Format("{0}_{1}__distances_psa.out",
                this.Quake.EventID ,
                this.Quake.StationCode)));
            sr.ReadLine();
            table.AddObject("Results", new CalculationResults()
            {
                PartitionKey = this.Quake.EventID,
                RowKey = this.Quake.StationCode,
                Start = start ,
                End = end,
                SimulationTime = (end-start).TotalMinutes , 
                Data = sr.ReadLine()
            });
            sr.Close();
            table.SaveChanges();

            foreach (var item in new System.IO.DirectoryInfo(this.Root).GetFiles(String.Format("{0}_{1}*.out",this.Quake.EventID ,this.Quake.StationCode) , SearchOption.AllDirectories))
            {
                var blob = this.Container.GetBlobReference(String.Format("{0}/{1}", this.Quake.StationCode  , item.Name));
                var file = Path.Combine(this.Root, item.Name);
                if (File.Exists(file))
                {
                    blob.UploadFile(file, new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(5) });
                    Trace.WriteLine(file);
                    File.Delete(file);
                }
            }
        }


    }
}
