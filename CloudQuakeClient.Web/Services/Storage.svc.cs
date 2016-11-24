using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient; 
using System.Xml.Linq;
using Model;

namespace CloudQuakeClient.Web.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Storage" in code, svc and config file together.
    public class Storage : IStorage
    {
        public string DoWork()
        {
            return System.DateTime.Now.ToShortTimeString();
        }


        public bool AddEventMessage(Model.QuakeMessage quake)
        {
            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==");
            try
            {
              var container =  service.CreateCloudBlobClient().GetContainerReference(quake.EventID);
              container.CreateIfNotExist();
              container.SetPermissions(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Blob });

            }
            catch (Exception)
            {
                return false;
            }
            var EventsQueue = service.CreateCloudQueueClient().GetQueueReference("earthquakes");
            EventsQueue.AddMessage(new CloudQueueMessage(quake.ToXml()));
  
            return true;
        }


        public List<string> GetStationPartitions()
        {
            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==");

            var data = service.CreateCloudTableClient().GetDataServiceContext().CreateQuery<StationsInfo>("Stations").AsTableServiceQuery().AsEnumerable().Select(x => x.PartitionKey).Distinct();

            return data.ToList();
                      

 
            
        }
    }
}
