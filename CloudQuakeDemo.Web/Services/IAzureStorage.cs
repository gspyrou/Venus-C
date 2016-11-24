using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Model;

namespace CloudQuakeDemo.Web.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IAzureStorage" in both code and config file together.
    [ServiceContract]
    public interface IAzureStorage
    {
        [OperationContract]
        bool AddMessage(QuakeMessage data);
        [OperationContract]
        List<string> GetStationsGroups();
        [OperationContract]
        List<StationsInfo> GetStations(string PartitionKey);
        [OperationContract]
        List<string> GetResults(string name);
        [OperationContract]
        bool CreateGrid(StationsInfo station, double MinLat, double MaxLat, double LatStep, double MinLon, double MaxLon, double LonStep);
        [OperationContract]
        bool UploadFile(string PartitionKey, string file);
        [OperationContract]
        QuakeMessage CalculateGeometry(QuakeMessage q);
        [OperationContract]
        void DeleteStation(string PartitionKey);
        
        
    }
}
