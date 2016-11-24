using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;
using Model;
using System.Xml.Serialization;

namespace PartitionWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        public override void Run()
        {
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            {
                configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
            });

            //Initialize 
            var storageAccount = CloudStorageAccount.FromConfigurationSetting("ConnectionString");
            var queueClient = storageAccount.CreateCloudQueueClient();
            var EventsQueue = queueClient.GetQueueReference("earthquakes");
            var StationsQueue = queueClient.GetQueueReference("stations");
            var ReducerQueue = queueClient.GetQueueReference("reducer");
            while (true)
            {
                Thread.Sleep(10000);
                CloudQueueMessage EventMessage = EventsQueue.GetMessage(TimeSpan.FromMinutes(2));
                if (EventMessage != null)
                {

                    QuakeMessage quake = DeserialeMessage(EventMessage);
                    quake.CalculateGeometry(); 
                 
                    quake.CalculateCenter();
                    var service = storageAccount.CreateCloudTableClient().GetDataServiceContext();
                    service.IgnoreResourceNotFoundException = true;
                    service.IgnoreMissingProperties = true;
                    IQueryable<StationsInfo> StationsList;
                    if (!String.IsNullOrEmpty(quake.StationsGroup))
                    {
                         StationsList = (from c in service.CreateQuery<StationsInfo>("Stations").AsTableServiceQuery()
                                            where c.PartitionKey == quake.StationsGroup
                                            select c);
                    }
                    else
                    {
                        StationsList = CalculateGrid(quake);
                    }
                    int Counter = 0;
                    foreach (var item in StationsList)
                    {
                        quake.StationCode = item.Code;
                        quake.Station = item;
                        CloudQueueMessage StationMessage = new CloudQueueMessage(quake.ToXml() );
                        StationsQueue.AddMessage(StationMessage);
                        Counter++;
                    }
                    CloudQueueMessage ReducerMessage = new CloudQueueMessage(String.Format("{0};{1};{2}",quake.EventID.ToLower()  ,Counter,quake.DeleteMe.ToString()) );
                    ReducerQueue.AddMessage(ReducerMessage);

                    EventsQueue.DeleteMessage(EventMessage);


                }
            }
        }

        private IQueryable<StationsInfo> CalculateGrid(QuakeMessage quake)
        {
            List<StationsInfo> result = new List<StationsInfo>();
            int iIndex=1;
            for (double i = quake.Lat-0.5; i <= quake.Lat+0.5; i+=0.1)
            {
                for (double j = quake.Lon - 0.5; j <= quake.Lon + 0.5; j += 0.1)
                {
                    result.Add(new StationsInfo()
                    {
                        Latitude = i,
                        Longitude = j,
                        Code = iIndex.ToString(),
                        CrustalAmplification = "crustal_amps_sample.txt",
                        SiteAmplification = "site_amps_d_klimis.txt",
                        SoilCondition = "soft-soil",
                        KappaCoeffient = 0.044
                    });
                    iIndex++;
 
                }
            }
            return result.AsQueryable();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
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
    }
}
