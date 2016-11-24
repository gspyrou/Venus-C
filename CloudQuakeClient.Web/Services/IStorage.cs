using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace CloudQuakeClient.Web.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IStorage" in both code and config file together.
    [ServiceContract]
    public interface IStorage
    {
        [OperationContract]
        string DoWork();
        [OperationContract]
        bool AddEventMessage(Model.QuakeMessage quake);
        [OperationContract]
        List<string> GetStationPartitions();

    }
}
