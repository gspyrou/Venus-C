using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using System.Configuration;


namespace Stations
{
    class Program
    {
        static void Main(string[] args)
        {
             // Get connection string from a configuration file.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccountConnectionString"]);
            CreateTableAndAddData(storageAccount);
        }
        static void CreateTableAndAddData(CloudStorageAccount storageAccount)
        {
            // Create service client for credentialed access to the Table service.
            CloudTableClient tableClient = new CloudTableClient(storageAccount.TableEndpoint.ToString(),
                storageAccount.Credentials);
            string tableName = "Stations";
            try
            {
                // Create a new table.
                tableClient.CreateTableIfNotExist(tableName);
                // Get data context.
                TableServiceContext context = tableClient.GetDataServiceContext();
                // Create the new entity

                StationsInfo station = new StationsInfo();
                station.Code = "xxx";
            }
            catch (Exception x)
            {
                Console.WriteLine(x.InnerException);
            }
        }

    }
}
