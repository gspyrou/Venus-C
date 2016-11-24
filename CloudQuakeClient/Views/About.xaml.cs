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
using CloudQuakeClient.ViewModel;

namespace CloudQuakeClient
{
    public partial class About : Page
    {
        public About()
        {
            InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CloudQuake.StorageClient client = new CloudQuake.StorageClient();
            client.GetStationPartitionsCompleted += (s, a) =>
            {
                foreach (var item in a.Result)
                {
                    MessageBox.Show(item);

                }
                           };
            client.GetStationPartitionsAsync ();     
        }
    }
}