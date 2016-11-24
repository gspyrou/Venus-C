using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Stations
{
    public class StationsInfo:TableServiceEntity
    {
        public StationsInfo():base("Stations",DateTime.UtcNow.Ticks.ToString("d19"))
        {
        }
        /// <summary>
        /// Station Code.
        /// </summary>
      
        public string Code { get; set; }

        /// <summary>
        /// Station name and description.
        /// </summary>
      
        public string Name { get; set; }

        /// <summary>
        /// Station Coordinates : Latitude.
        /// </summary>
      
        public double Latitude { get; set; }
        /// <summary>
        /// Station Coordinates : Longitude.
        /// </summary>

        public double Longitude { get; set; }

        /// <summary>
        /// Definition of soild conditions.
        /// </summary>

        public double KappaCoeffient { get; set; }

        public string SoilCondition { get; set; }

        public string  CrustalAmplification { get; set; }

        public string  SiteAmplification { get; set; }
    }
}
