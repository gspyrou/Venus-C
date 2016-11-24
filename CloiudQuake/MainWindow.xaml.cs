using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Symbols;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using System.Configuration;
using System.IO;
using Model;
using System.Threading;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace CloiudQuake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        private double Lat;
        private double Lon;
        public MainWindow()
        {
            InitializeComponent();
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            {
                configSetter(ConfigurationManager.AppSettings[configName]);
            });
        }

        private void MyMap_MouseClick(object sender, ESRI.ArcGIS.Client.Map.MouseEventArgs e)
        {
            this.Latitude.Text = e.MapPoint.Y.ToString();
            this.Longitude.Text = e.MapPoint.X.ToString();

            GraphicsLayer graphicsLayer = MyMap.Layers["Data"] as GraphicsLayer;

            graphicsLayer.Graphics.Clear();

            Graphic point = new Graphic();
            point.Geometry = e.MapPoint;
            point.Symbol = new SimpleMarkerSymbol()
            {
                Color = new SolidColorBrush(Colors.Red),
                Size = 10,
                Style = SimpleMarkerSymbol.SimpleMarkerStyle.Circle
            };

            graphicsLayer.Graphics.Add(point);

            this.SendMessage.IsEnabled = true;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(this.EventID.Text))
            {

                MessageBox.Show("Event ID can not be empty !\n Please enter a valid name for the Event ID."
                    , "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string message = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}", this.EventID.Text.ToLower(),
                                                this.Magnitude.Text,
                                                this.Latitude.Text,
                                                this.Longitude.Text,
                                                this.Depth.Text,
                                                this.Strike.Text,
                                                this.Dip.Text,
                                                this.FaultType.Text,
                                                this.Flag.Text,
                                                this.Stress.Text,
                                                this.Stations.Text);


            QuakeMessage data = new QuakeMessage()
            {
                Depth = Convert.ToDouble(this.Depth.Text) ,
                Dip = Convert.ToDouble(this.Dip.Text),
                Stress = Convert.ToDouble(this.Stress.Text),
                Strike = Convert.ToDouble(this.Strike.Text) ,
                FaultType = "N",
                Flag = "1",
                StationsGroup = this.Stations.Text  ,
                Lat = Convert.ToDouble(this.Latitude.Text),
                Lon = Convert.ToDouble(this.Longitude.Text)   ,
                Magnitude = Convert.ToDouble(this.Magnitude.Text  ),
                EventID = this.EventID.Text  
            };
            var storageAccount = CloudStorageAccount.FromConfigurationSetting("ConnectionString");
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("earthquakes");

            try
            {
                var blobClient = storageAccount.CreateCloudBlobClient();
                var blobContainer = blobClient.GetContainerReference(data.EventID.ToLower());
                blobContainer.CreateIfNotExist();
                blobContainer.SetPermissions(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Blob });

            }
            catch (Exception)
            {

                MessageBox.Show("The blob container could not be created (check if the same event name already exist in Blob Storage"
                    , "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CloudQueueMessage q = new CloudQueueMessage(data.ToXml());
            queue.AddMessage(q);

            MessageBox.Show(String.Format("The message for event '{0}' has been sucessfully sent to Azure Queue", this.EventID.Text)
                 , "Information", MessageBoxButton.OK, MessageBoxImage.Information);

            //this.SendMessage.IsEnabled = false;
            (MyMap.Layers["Data"] as GraphicsLayer).Graphics.Clear();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                AddStation(dlg);
            }

        }

        void dispOp_Completed(object sender, EventArgs e)
        {
            MessageBox.Show("Done");
        }

        private void AddStation(Microsoft.Win32.OpenFileDialog dlg)
        {
            var account = CloudStorageAccount.FromConfigurationSetting("ConnectionString");
            // Open document
            string filename = dlg.FileName;

            var tableclient = account.CreateCloudTableClient();

            var table = tableclient.GetDataServiceContext();

            StreamReader sr = new StreamReader(filename);
            //Skip header
            // sr.ReadLine();

            string line;
            string[] cells;
            ObservableCollection<string> d = new ObservableCollection<string>();
            this.StationsList.ItemsSource = d; 
            while ((line = sr.ReadLine()) != null)
            {
                cells = line.Split(new char[] { ' ', '\t',',' }, StringSplitOptions.RemoveEmptyEntries);

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
                    PartitionKey = this.GroupName.Text
                };

                table.AddObject("Stations", station);
                table.SaveChanges();

                d.Add(line);


            }
            sr.Close();
        }
    }
}
