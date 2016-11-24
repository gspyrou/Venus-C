using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Model
{
    public class CalculationResults : TableServiceEntity
    {
        public CalculationResults()
            : base("", DateTime.UtcNow.Ticks.ToString("d19"))
        {
        }

        public DateTime  Start { get; set; }
        public DateTime  End { get; set; }
        public string Data { get; set; }
        public double SimulationTime { get; set; }

    }
}
