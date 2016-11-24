using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Model;

namespace CloudQuakeDemo.Web.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "AzureStorage" in code, svc and config file together.
    public class AzureStorage : IAzureStorage
    {
        public bool AddMessage(QuakeMessage data)
        {
            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==");
            try
            {
                var container = service.CreateCloudBlobClient().GetContainerReference(data.EventID.ToLower() );
                container.CreateIfNotExist();
                container.SetPermissions(new BlobContainerPermissions()
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
                var queue = service.CreateCloudQueueClient().GetQueueReference("earthquakes");
                queue.AddMessage(new CloudQueueMessage(data.ToXml()));

                return true;
            }
            catch (Exception)
            {
                return false;
            }


        }

        public List<string> GetStationsGroups()
        {
            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==");

            var data = service.CreateCloudTableClient().GetDataServiceContext().CreateQuery<StationsInfo>("Stations").AsTableServiceQuery().AsEnumerable().Select(x => x.PartitionKey).Distinct();
            return data.ToList();
        }

        public List<StationsInfo> GetStations(string PartitionKey)
        {
            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==");
            var data = from c in service.CreateCloudTableClient().GetDataServiceContext().CreateQuery<StationsInfo>("Stations").AsTableServiceQuery()
                       where c.PartitionKey == PartitionKey
                       select c;
            return data.ToList();
        }


        public List<string> GetResults(string name)
        {
            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==");
            var data = from c in service.CreateCloudTableClient().GetDataServiceContext().CreateQuery<CalculationResults>("Results").AsTableServiceQuery()
                       where c.PartitionKey == name
                       select c;
            var result = new List<string>();
            foreach (var item in data)
	        {
                result.Add(item.RowKey);
		 
	        }
            return result;
        }


        public bool CreateGrid(StationsInfo station, double MinLat, double MaxLat, double LatStep, double MinLon, double MaxLon, double LonStep)
        {
            int iIndex = 1;
            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==");
            var table = service.CreateCloudTableClient().GetDataServiceContext(); 
            for (double i = MinLat ; i <= MaxLat; i+=LatStep )
            {
                for (double j = MinLon ; j < MaxLon; j+=LonStep )
                {
                    StationsInfo item = new StationsInfo()
                    {
                        RowKey = DateTime.UtcNow.Ticks.ToString("d19"),
                        Latitude = i,
                        Longitude = j,
                        Code = iIndex.ToString(),
                        Name = iIndex.ToString(),
                        KappaCoeffient = station.KappaCoeffient,
                        CrustalAmplification = station.CrustalAmplification,
                        PartitionKey = station.PartitionKey,
                        SiteAmplification = station.SiteAmplification,
                        SoilCondition = station.SoilCondition,
                        Elevation = 0

                    };
                    table.AddObject("Stations", item);
                    table.SaveChanges();
                    iIndex++;
                    
                }
            }
            return true;
        }


        public bool UploadFile(string PartitionKey, string file)
        {

            string[] lines = file.Split('\n');
            string[] cells;

            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==");
            var table = service.CreateCloudTableClient().GetDataServiceContext();

            foreach (var item in lines)
            {
                cells = item.Split(new char[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

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
                    Elevation = 0,
                    PartitionKey = PartitionKey
                };

                table.AddObject("Stations", station);
                table.SaveChanges();

            }
            return true;
        }


        public QuakeMessage CalculateGeometry(QuakeMessage q)
        {
            q.CalculateGeometry();
            q.CalculateCenter();
            return q;
        }


        public void DeleteStation(string PartitionKey)
        {
            var service = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cloudquake;AccountKey=Mm+fR9oqs12+fF/96ThXUtl3eMCkMlYMRvTf+dEP36dQRNqS6oKQoRrtELTF4AWaPEISkWsMBzo65ZI+PU2LGA==").CreateCloudTableClient().GetDataServiceContext();
            var data = from c in service.CreateQuery<StationsInfo>("Stations").AsTableServiceQuery()
                       where c.PartitionKey == PartitionKey
                       select c;          
            foreach (var item in data)
            {
                service.DeleteObject(item);
                service.SaveChanges(); 
            }
            return;
        }
    }
}
