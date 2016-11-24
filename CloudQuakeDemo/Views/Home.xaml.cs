using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.FeatureService.Symbols;
using CloudQuakeDemo.ViewModel;
using ESRI.ArcGIS.Client.Geometry;
using CloudQuakeDemo.Services;
using System.Threading;
using System.Windows.Threading;

namespace CloudQuakeDemo
{
    public partial class Home : Page
    {
        private DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
        private DateTime Start;

        public Home()
        {
            InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            timer.Tick += (s, a) =>
                {
                    (this.DataContext as MainViewModel).Duration = System.DateTime.Now - this.Start;
                    Services.AzureStorageClient client = new AzureStorageClient();
                    client.GetResultsCompleted += (se, args) =>
                        {
                            foreach (var item in args.Result)
                            {
                                UpdateStation(item);
                            }
                            if (args.Result.Count == (MyMap.Layers["Stations"] as GraphicsLayer).Graphics.Count)
                            {
                                timer.Stop();
                                this.DownloadData.IsEnabled = true;
                                this.DownloadMap.IsEnabled = true;
                                //(this.DataContext as MainViewModel).IsBusy = false;
         
                            }
                        };
                    client.GetResultsAsync((this.DataContext as MainViewModel).Quake.EventID );
                };
        }

        private void UpdateStation(string name)
        {
            var graphics = (MyMap.Layers["Stations"] as GraphicsLayer).Graphics;

            var item = (from c in graphics
                        where c.Attributes.ElementAt(0).Value.ToString() == name
                        select c).FirstOrDefault();

            if (item.Attributes["Status"].ToString() == "Pending")
            {
                item.Symbol = new SimpleMarkerSymbol()
                {
                    Color = new SolidColorBrush(Colors.Green ),
                    Size = 10,
                    OutlineColor = new SolidColorBrush(Colors.Black),
                    OutlineThickness = 1,
                    Style = SimpleMarkerSymbol.SimpleMarkerStyle.Square
                };
                item.Attributes["Status"] = "Done";
            }


        }

        private void MyMap_MouseClick(object sender, ESRI.ArcGIS.Client.Map.MouseEventArgs e)
        {
            GraphicsLayer graphicsLayer = MyMap.Layers["Data"] as GraphicsLayer;

            graphicsLayer.Graphics.Clear();

            Graphic point = new Graphic();
            point.Geometry = e.MapPoint;
            point.Symbol = LayoutRoot.Resources["CustomStrobeMarkerSymbol"] as ESRI.ArcGIS.Client.Symbols.Symbol;

            graphicsLayer.Graphics.Add(point);

            (this.DataContext as MainViewModel).Quake.Lat = e.MapPoint.Y;
            (this.DataContext as MainViewModel).Quake.Lon = e.MapPoint.X;
            this.Lat.Text = Math.Round(e.MapPoint.Y,5).ToString();
            this.Longitude.Text = Math.Round(e.MapPoint.X, 5).ToString();
            (this.DataContext as MainViewModel).IsLoading = true;
           
            Services.AzureStorageClient client = new AzureStorageClient();
            client.CalculateGeometryCompleted += (s, a) =>
                {
                    (this.DataContext as MainViewModel).Quake = a.Result;
                    (this.DataContext as MainViewModel).IsLoading = false;
                    GraphicsLayer graphics = MyMap.Layers["Data"] as GraphicsLayer;

             
                    Graphic polygon = new Graphic();
                    ESRI.ArcGIS.Client.Geometry.PointCollection pointCollection1 = new ESRI.ArcGIS.Client.Geometry.PointCollection();
                    pointCollection1.Add(new MapPoint(a.Result.LonCalc, a.Result.LatCalc, new SpatialReference(4326)));
                    pointCollection1.Add(new MapPoint(a.Result.LonCw, a.Result.LatCw, new SpatialReference(4326)));
                    pointCollection1.Add(new MapPoint(a.Result.LonCw2, a.Result.LatCw2, new SpatialReference(4326)));
                    pointCollection1.Add(new MapPoint(a.Result.LonCalc2, a.Result.LatCalc2, new SpatialReference(4326)));
                  
                    pointCollection1.Add(new MapPoint(a.Result.LonCalc, a.Result.LatCalc, new SpatialReference(4326)));
                  
                    ESRI.ArcGIS.Client.Geometry.Polygon polygon1 = new ESRI.ArcGIS.Client.Geometry.Polygon();
                    polygon1.Rings.Add(pointCollection1);


                    polygon.Symbol = LayoutRoot.Resources["DefaultFillSymbol"] as ESRI.ArcGIS.Client.Symbols.Symbol;
                    polygon.Geometry = polygon1 ;

                    graphics.Graphics.Add(polygon);
                   // graphics.Graphics.Add(graphics.Graphics[0]);

                    ESRI.ArcGIS.Client.Geometry.Polyline polyline = new ESRI.ArcGIS.Client.Geometry.Polyline();
                    ESRI.ArcGIS.Client.Geometry.PointCollection pointCollection2 = new ESRI.ArcGIS.Client.Geometry.PointCollection();
                    pointCollection2.Add(new MapPoint(a.Result.LonCalc, a.Result.LatCalc, new SpatialReference(4326)));
                    pointCollection2.Add(new MapPoint(a.Result.LonCalc2, a.Result.LatCalc2, new SpatialReference(4326)));
                  
                    polyline.Paths.Add(pointCollection2);

                    Graphic line = new Graphic()
                    {
                        Symbol = LayoutRoot.Resources["DefaultLineSymbol"] as ESRI.ArcGIS.Client.Symbols.Symbol,

                        Geometry = polyline
                    };
                    graphics.Graphics.Add(line);

                   // GetStations();
                  
                };
            client.CalculateGeometryAsync((this.DataContext as MainViewModel).Quake);

        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)Automatic.IsChecked)
            {
                (this.DataContext as MainViewModel).Quake.StationsGroup = String.Empty;
                GraphicsLayer stations = MyMap.Layers["Stations"] as GraphicsLayer;
                stations.Graphics.Clear();
                int iIndex = 1;
                for (double i = (this.DataContext as MainViewModel).Quake.Lat - 0.5; i <= (this.DataContext as MainViewModel).Quake.Lat + 0.5; i += 0.1)
                {
                    for (double j = (this.DataContext as MainViewModel).Quake.Lon - 0.5; j <= (this.DataContext as MainViewModel).Quake.Lon + 0.5; j += 0.1)
                    {
                        Graphic p = new Graphic();
                        p.Geometry = new MapPoint(j, i);
                        p.Symbol = new SimpleMarkerSymbol()
                        {
                            Color = new SolidColorBrush(Colors.Yellow),
                            Size = 10,
                            OutlineColor = new SolidColorBrush(Colors.Black),
                            OutlineThickness = 1,
                            Style = SimpleMarkerSymbol.SimpleMarkerStyle.Square
                        };
                        p.Attributes.Add("ID", iIndex.ToString());
                        p.Attributes.Add("Status", "Pending");
                        stations.Graphics.Add(p);
                        iIndex++;

                    }

                }
                MyMap.Extent = (MyMap.Layers["Stations"] as GraphicsLayer).FullExtent;
                (this.DataContext as MainViewModel).IsLoading = false;
            }
            Services.AzureStorageClient client = new Services.AzureStorageClient();
            client.AddMessageCompleted += (s, a) =>
                {
                    timer.Start();
                    this.Start = System.DateTime.Now;
                    (this.DataContext as MainViewModel).IsBusy = true;
                    
                };
            (this.DataContext as MainViewModel).Quake.EventID = (this.DataContext as MainViewModel).Quake.EventID.ToLower();  
            
            client.AddMessageAsync((this.DataContext as MainViewModel).Quake);
            
        }
        private void GetStations()
        {
            (this.DataContext as MainViewModel).IsLoading = true;
            if (!(bool)this.Automatic.IsChecked)
            {
                Services.AzureStorageClient client = new AzureStorageClient();
                client.GetStationsCompleted += (s, a) =>
                {
                    (MyMap.Layers["Stations"] as GraphicsLayer).Graphics.Clear();
                    foreach (var item in a.Result)
                    {
                        Graphic point = new Graphic();
                        point.Geometry = new MapPoint(item.Longitude, item.Latitude, new SpatialReference(4326));
                        point.Symbol = new SimpleMarkerSymbol()
                        {
                            Color = new SolidColorBrush(Colors.Yellow),
                            Size = 10,
                            OutlineColor = new SolidColorBrush(Colors.Black),
                            OutlineThickness = 1,
                            Style = SimpleMarkerSymbol.SimpleMarkerStyle.Square
                        };
                        point.Attributes.Add("ID", item.Code);
                        point.Attributes.Add("Status", "Pending");
                        (MyMap.Layers["Stations"] as GraphicsLayer).Graphics.Add(point);

                    }

                    MyMap.Extent = (MyMap.Layers["Stations"] as GraphicsLayer).FullExtent;
                    (this.DataContext as MainViewModel).IsLoading = false;

                };
                client.GetStationsAsync((this.DataContext as MainViewModel).Quake.StationsGroup);

            }
        }

        private void StationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetStations();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as MainViewModel).IsBusy = false;
            this.DownloadData.IsEnabled = false;
            this.DownloadMap.IsEnabled = false ;
            (MyMap.Layers["Stations"] as GraphicsLayer).Graphics.Clear();
            (MyMap.Layers["Data"] as GraphicsLayer).Graphics.Clear();
            if (this.timer.IsEnabled)
                timer.Stop();
        }

        private void Automatic_Click(object sender, RoutedEventArgs e)
        {
            (MyMap.Layers["Stations"] as GraphicsLayer).Graphics.Clear();
            StationsList.IsEnabled = false; 
        }

        private void Defined_Click(object sender, RoutedEventArgs e)
        {
            StationsList.IsEnabled = true;
            (MyMap.Layers["Stations"] as GraphicsLayer).Graphics.Clear();
           
        }

    }
}
