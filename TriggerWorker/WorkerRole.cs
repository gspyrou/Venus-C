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

namespace TriggerWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
        //    Trace.WriteLine("TriggerWorker entry point called", "Information");

            try
            {



                CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                {
                    configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
                });


                var storageAccount = CloudStorageAccount.FromConfigurationSetting("ConnectionString");
                var queueClient = storageAccount.CreateCloudQueueClient();
                var EventsQueue = queueClient.GetQueueReference("earthquakes");
                var StationsQueue = queueClient.GetQueueReference("stations");
                var service = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("ConnectionString")).CreateCloudTableClient().GetDataServiceContext();
                var stations = from c in service.CreateQuery<StationsInfo>("Stations").AsTableServiceQuery().Take(14) 
                               select c;
                while (true)
                {
                    Thread.Sleep(10000);
                    CloudQueueMessage eventMessage = EventsQueue.GetMessage();
                    string[] data;
                    if (eventMessage != null)
                    {
                        data = eventMessage.AsString.Split(';');
                        Trace.WriteLine(data[0], "Information");
                        Trace.WriteLine(data[1], "Information");
                        foreach (var item in stations)
                        {
                            CloudQueueMessage stationMessage = new CloudQueueMessage(
                                String.Format("{0};{1}",
                                            eventMessage.AsString,
                                            item.Code));
                            StationsQueue.AddMessage(stationMessage);

                        }
                        EventsQueue.DeleteMessage(eventMessage);
                    }
                }
            }
            catch (Exception x)
            {

                System.Diagnostics.Trace.WriteLine(x.InnerException) ;
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
