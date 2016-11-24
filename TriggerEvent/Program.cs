using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using System.Configuration;
using System.Threading;
using System.IO;
using Model;
using System.Xml.Serialization;

namespace TriggerEvent
{
    class Program
    {
       
        static void Main(string[] args)
        {
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            {
                configSetter(ConfigurationManager.AppSettings[configName]);
            });

            var storageAccount = CloudStorageAccount.FromConfigurationSetting("ConnectionString");
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("earthquakes");
            queue.CreateIfNotExist();
          
           var s =  queueClient.GetQueueReference("stations");
           //s.Delete();
            s.CreateIfNotExist();

            var d = queueClient.GetQueueReference("reducer");
           // d.Delete();
            d.CreateIfNotExist();  

            InitializeStationsTable(storageAccount );
            var service = storageAccount.CreateCloudTableClient().GetDataServiceContext();  
            var stations = from c in service.CreateQuery<StationsInfo>("Stations").AsTableServiceQuery()
                           select c;

            var ConfigurationContainer = storageAccount.CreateCloudBlobClient().GetContainerReference("config");


            StreamWriter sw = new StreamWriter("sta_grid.txt");
            int i=0;
            foreach (var item in stations )
            {
                switch (item.SoilCondition.ToLower() )
                {
                    case "rock":
                        i = 0;
                        break;
                    case "soil":
                        i = 1;
                        break;
                    case "soft-soil":
                        i = 2;
                        break;
                   
                }
                sw.WriteLine(String.Format("{0} {1} {2}",item.Latitude , item.Longitude , i));
   
            }
            sw.Close();
             
            ConfigurationContainer.GetBlobReference("sta_grid.txt").UploadFile("sta_grid.txt");
            string message = String.Empty;
            QuakeMessage data = new QuakeMessage()
            {
                Depth = 0.7,
                Dip = 43,
                Stress = 55,
                Strike = 88,
                FaultType = "N",
                Flag = "1",
                StationsGroup = "benchmark",
                Lat = 40.45,
                Lon = 23,
                Magnitude = 5.1,
                EventID = "testmessage"
            };

            
            queue.AddMessage(new CloudQueueMessage(data.ToXml() ));
            
            System.Threading.Thread.Sleep(5000);

            using (Stream stream = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(queue.GetMessage().AsString  );
                writer.Flush();

                stream.Position = 0;
                object result = new XmlSerializer(typeof(QuakeMessage)).Deserialize(stream);

                QuakeMessage  ddd = (QuakeMessage)result;
                Console.WriteLine(ddd.StationsGroup); 
            }

            QuakeMessage ss = new QuakeMessage();
           
            //queue.GetMessage().AsString()   
            
            string title;
            while (true)
            {
                message = String.Empty;
                title= String.Empty;
                Console.WriteLine("Title : ");
                title = Console.ReadLine();
                message+= title+";";
                Console.WriteLine("Mangitude : ");
                message += Console.ReadLine() + ";";
                Console.WriteLine("Latitude : ");
                message += Console.ReadLine() + ";";
                Console.WriteLine("Longitude");
                message += Console.ReadLine()+";";
                Console.WriteLine("Depth : ");
                message += Console.ReadLine();
              //  CloudQueueMessage m = new CloudQueueMessage(data.ToBinary());
                // queue.AddMessage(new CloudQueueMessage(message));
                Console.ReadKey();
                try
                {
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    var blobContainer = blobClient.GetContainerReference(title);
                    blobContainer.CreateIfNotExist();
                    blobContainer.SetPermissions(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Blob });
                }
                catch (Exception x)
                {
                    Console.WriteLine(x.InnerException.Message);
                }
               
            }

        }

        private static void InitializeStationsTable(CloudStorageAccount account)
        {
            var tableclient = account.CreateCloudTableClient();
            if (tableclient.CreateTableIfNotExist("Stations"))
            {
                var table = tableclient.GetDataServiceContext();

                StreamReader sr = new StreamReader("rock_stations.txt");
                //Skip header
               // sr.ReadLine();

                string line;
                string[] cells;
                while ((line = sr.ReadLine()) != null)
                {

                    
                    cells = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    Console.WriteLine(cells[0]);
                    StationsInfo station = new StationsInfo()
                    {
                        Code = cells[0],
                        Name = String.Format("{0} description", cells[0]),
                        Latitude = Convert.ToDouble(cells[1]),
                        Longitude = Convert.ToDouble(cells[2]),
                        SoilCondition = cells[3],
                        KappaCoeffient = Convert.ToDouble(cells[4]),
                        CrustalAmplification = cells[5],
                        SiteAmplification = cells[6],
                        Elevation =0,
                        PartitionKey ="santorinirock"
                    };

                    table.AddObject("Stations", station);
                    table.SaveChanges();
                }
            }
        }
    }
}
