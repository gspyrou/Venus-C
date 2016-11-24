using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace Model
{
    
    public class QuakeMessage  
    {
        public string  EventID { get; set; }
        public double  Magnitude { get; set; }
        public double  Lat { get; set; }
        public double  Lon { get; set; }
        public double LatCalc { get; set; }
        public double LonCalc { get; set; }
        public double LatCalc2 { get; set; }
        public double LonCalc2 { get; set; }
        public double LatCw { get; set; }
        public double LonCw { get; set; }
        public double LatCw2 { get; set; }
        public double LonCw2 { get; set; }       
        public double  Strike { get; set; }
        public double  Dip { get; set; }
        public double  Depth { get; set; }
        public double  TopDepth { get; set; }
        public double  Stress { get; set; }
        public double  Length { get; set; }
        public double  Width { get; set; }
        public string  Flag { get; set; }
        public string  FaultType { get; set; }
        public string  StationsGroup { get; set; }
        public string  StationCode { get; set; }
        public bool  DeleteMe { get; set; }
        public StationsInfo  Station { get; set; }

        public QuakeMessage()
        {
        }
        public void CalculateGeometry()
        {
            if (this.Length == 0)
            {
                switch (this.FaultType )
                {
                    case "N":
                        this.Length =Math.Round( Math.Pow(10,(0.50 * this.Magnitude - 1.86)),1);
                        break;
                    case "S":
                        this.Length =Math.Round( Math.Pow(10,(0.59 * this.Magnitude - 2.30)),1);
                        break;
                    case "T":
                        this.Length =Math.Round( Math.Pow(10,(0.55 * this.Magnitude - 2.19)),1);
                        break;
                    default:
                        break;
                }
            }
            if (this.Width == 0)
            {
                switch (this.FaultType)
                {
                    case "N":
                        this.Width = Math.Round(Math.Pow(10,(0.28 * this.Magnitude - 0.70)),1);
                        break;
                    case "S":
                        this.Width =Math.Round(Math.Pow(10,(0.23 * this.Magnitude - 0.49)),1);
                        break;
                    case "T":
                        this.Width = Math.Round(Math.Pow(10,(0.23 * this.Magnitude - 0.49)),1);
                        break;
                    default:
                        break;
                }
            }

        }
        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
        private double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }
        public void CalculateCenter()
        {
            double WW = this.Width * Math.Cos(DegreeToRadian(this.Dip));
            double Lat_tmp = this.Lat;
            double Lon_tmp = this.Lon;
            this.Lat = Math.Round(RadianToDegree(Math.Asin(Math.Sin(DegreeToRadian(Lat_tmp)) * Math.Cos(0.5 * WW / 6371) + Math.Cos(DegreeToRadian(Lat_tmp)) * Math.Sin(0.5 * WW / 6371) * Math.Cos(DegreeToRadian(this.Strike - 90)))),5);
            this.Lon = Math.Round(Lon_tmp + RadianToDegree(Math.Atan2(Math.Sin(DegreeToRadian(this.Strike - 90)) * Math.Sin(0.5 * WW / 6371) * Math.Cos(DegreeToRadian(this.Lat)),
                Math.Cos (0.5*WW/6371)- Math.Sin (DegreeToRadian (Lat_tmp))*Math.Sin (DegreeToRadian (this.Lat)))),5);
            this.LatCalc =Math.Round( RadianToDegree(Math.Asin(Math.Sin(DegreeToRadian(this.Lat)) * Math.Cos(this.Length*0.5 / 6371) - Math.Cos(DegreeToRadian(this.Lat)) * Math.Sin(this.Length*0.5 / 6371) * Math.Cos(DegreeToRadian(this.Strike)))),5);
            this.LonCalc =Math.Round( this.Lon + RadianToDegree(Math.Atan2(
                -Math.Sin(DegreeToRadian(this.Strike)) * Math.Sin(this.Length*0.5 / 6371) * Math.Cos(DegreeToRadian(Lat)),
                Math.Cos(this.Length*0.5 / 6371) - Math.Sin(DegreeToRadian(this.Lat)) * Math.Sin(DegreeToRadian(this.LatCalc)))),5);
            this.LatCalc2 = Math.Round( 2 * this.Lat - this.LatCalc,5);
            this.LonCalc2 = Math.Round( 2 * this.Lon - this.LonCalc,5);
            this.LatCw =Math.Round( RadianToDegree(Math.Asin(Math.Sin(DegreeToRadian(this.LatCalc)) * Math.Cos(WW / 6371) + Math.Cos(DegreeToRadian(this.LatCalc)) * Math.Sin(WW / 6371) * Math.Cos(DegreeToRadian(this.Strike + 90)))),5);
            this.LonCw =Math.Round( this.LonCalc + RadianToDegree(Math.Atan2(Math.Sin(DegreeToRadian(this.Strike + 90)) * Math.Sin(WW / 6371) * Math.Cos(DegreeToRadian(this.LatCalc)), Math.Cos(WW / 6371) - Math.Sin(DegreeToRadian(this.LatCalc)) * Math.Sin(DegreeToRadian(this.LatCw)))),5);

            this.LatCw2 =Math.Round( RadianToDegree(Math.Asin(Math.Sin(DegreeToRadian(this.LatCalc2)) * Math.Cos(WW / 6371) + Math.Cos(DegreeToRadian(this.LatCalc2)) * Math.Sin(WW / 6371) * Math.Cos(DegreeToRadian(this.Strike + 90)))),5);
            this.LonCw2 =Math.Round( this.LonCalc2 + RadianToDegree(Math.Atan2( Math.Sin(DegreeToRadian(this.Strike + 90)) * Math.Sin(WW / 6371) * Math.Cos(DegreeToRadian(this.LatCalc2)),Math.Cos(WW / 6371) - Math.Sin(DegreeToRadian(this.LatCalc2)) * Math.Sin(DegreeToRadian(this.LatCw2)))),5);

            this.TopDepth = Math.Round(this.Depth - (this.Width / 2) * Math.Sin(DegreeToRadian(this.Dip)),1);
        }
        public string ToXml()
        {
            using (Stream steam = new MemoryStream())
            {
                new XmlSerializer(this.GetType()).Serialize(steam, this);

                steam.Position = 0;
                string result = new StreamReader(steam).ReadToEnd();

                return result;
            }
        }
        public static QuakeMessage FromXml(string xml)
        {
            using (Stream stream = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(xml);
                writer.Flush();

                stream.Position = 0;
                object result = new XmlSerializer(typeof(QuakeMessage)).Deserialize(stream);

                return (QuakeMessage)result;
            }
        }
    }
}
