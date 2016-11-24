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
using CloudQuakeDemo.ViewModel;

namespace CloudQuakeDemo.Views
{
    public partial class Stations : Page
    {
        public Stations()
        {
            InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void StationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (this.DataContext as MainViewModel).IsLoading = true;
            Services.AzureStorageClient client = new Services.AzureStorageClient();
            client.GetStationsCompleted += (s, a) =>
                {
                    this.DataGrid.Visibility = System.Windows.Visibility.Visible;
                    this.Info.Visibility = System.Windows.Visibility.Visible;
                    this.DataGrid.ItemsSource = a.Result;
                    (this.DataContext as MainViewModel).IsLoading = false;
                };
            client.GetStationsAsync(StationsList.SelectedItem.ToString());  
            //this.DataGrid.ItemsSource = 
        }

        private void CreateGrid_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as MainViewModel).IsLoading = true;
            Services.AzureStorageClient client = new Services.AzureStorageClient();

            client.CreateGridCompleted += (s, a) =>
            {
                (this.DataContext as MainViewModel).GetStationsGroups(); 
            };
            client.CreateGridAsync((this.DataContext as MainViewModel).StationData,
                                    Convert.ToDouble(this.MinLat.Text),
                                    Convert.ToDouble(this.MaxLat.Text),
                                    Convert.ToDouble(this.StepLat.Text),
                                    Convert.ToDouble(this.MinLon.Text),
                                    Convert.ToDouble(this.MaxLon.Text),
                                    Convert.ToDouble(this.StepLon.Text));
            
        }

        private void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = true;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                // Open the selected file to read.
                System.IO.Stream fileStream = openFileDialog1.File.OpenRead();

                using (System.IO.StreamReader reader = new System.IO.StreamReader(fileStream))
                {
                    (this.DataContext as MainViewModel).IsLoading = true;
                    Services.AzureStorageClient client = new Services.AzureStorageClient();
                    client.UploadFileCompleted += (s, a) =>
                        {
                            (this.DataContext as MainViewModel).GetStationsGroups();
                            MessageBox.Show("File was imported sucessfully");
                          
                            //this.StationsList.SelectedItem = (this.DataContext as MainViewModel).StationData.PartitionKey;
                        };
                    client.UploadFileAsync((this.DataContext as MainViewModel).StationData.PartitionKey, reader.ReadToEnd()  );   
                }
                fileStream.Close();
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as MainViewModel).IsLoading = true;
            Services.AzureStorageClient client = new Services.AzureStorageClient();
            client.DeleteStationCompleted += (s, a) =>
                {
                    (this.DataContext as MainViewModel).IsLoading = false;
                    this.DataGrid.Visibility = System.Windows.Visibility.Collapsed ;
                    this.Info.Visibility = System.Windows.Visibility.Collapsed ;
                   
                };
            client.DeleteStationAsync(this.StationsList.SelectedItem.ToString()); 
        }

    }
}
