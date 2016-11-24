using GalaSoft.MvvmLight;
using CloudQuakeDemo.Services;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.FeatureService.Symbols;
using System.Windows.Media;
using System;
using System.Collections.Generic;

namespace CloudQuakeDemo.ViewModel
{
   
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// The <see cref="IsLoading" /> property's name.
        /// </summary>
        public const string IsLoadingPropertyName = "IsLoading";

        private bool _isloading = false;

        /// <summary>
        /// Sets and gets the IsLoading property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsLoading
        {
            get
            {
                return _isloading;
            }

            set
            {
                if (_isloading == value)
                {
                    return;
                }

                _isloading = value;
                RaisePropertyChanged(IsLoadingPropertyName);
            }
        }
        /// <summary>
        /// The <see cref="StationData" /> property's name.
        /// </summary>
        public const string StationDataPropertyName = "StationData";

        private StationsInfo _stationdata = null;

        /// <summary>
        /// Sets and gets the StationData property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public StationsInfo StationData
        {
            get
            {
                return _stationdata;
            }

            set
            {
                if (_stationdata == value)
                {
                    return;
                }

                _stationdata = value;
                RaisePropertyChanged(StationDataPropertyName);
            }
        }
        /// <summary>
        /// The <see cref="DeleteMe" /> property's name.
        /// </summary>
        public const string DeleteMePropertyName = "DeleteMe";

        private bool _deleteme = false;

        /// <summary>
        /// Sets and gets the DeleteMe property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool DeleteMe
        {
            get
            {
                return _deleteme;
            }

            set
            {
                if (_deleteme == value)
                {
                    return;
                }

                _deleteme = value;
                RaisePropertyChanged(DeleteMePropertyName);
            }
        }
        /// <summary>
        /// The <see cref="Duration" /> property's name.
        /// </summary>
        public const string DurationPropertyName = "Duration";

        private TimeSpan _duration ;

        /// <summary>
        /// Sets and gets the Duration property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return _duration;
            }

            set
            {
                if (_duration == value)
                {
                    return;
                }

                _duration = value;
                RaisePropertyChanged(DurationPropertyName);
            }
        }
        /// <summary>
        /// The <see cref="IsBusy" /> property's name.
        /// </summary>
        public const string IsBusyPropertyName = "IsBusy";

        private bool _isbusy = false;

        /// <summary>
        /// Sets and gets the IsBusy property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return _isbusy;
            }

            set
            {
                if (_isbusy == value)
                {
                    return;
                }

                _isbusy = value;
                RaisePropertyChanged(IsBusyPropertyName);
            }
        }
        /// <summary>
        /// The <see cref="Quake" /> property's name.
        /// </summary>
        public const string QuakePropertyName = "Quake";

        private QuakeMessage _quake = null;

        /// <summary>
        /// Sets and gets the Quake property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public QuakeMessage Quake
        {
            get
            {
                return _quake;
            }

            set
            {
                if (_quake == value)
                {
                    return;
                }

                _quake = value;
                RaisePropertyChanged(QuakePropertyName);
            }
        }
        public ObservableCollection<string> StationGroups { get; set; }
        /// <summary>
        /// The <see cref="Stations" /> property's name.
        /// </summary>
        public const string StationsPropertyName = "Stations";

        private ObservableCollection<Graphic> _stations = null ;

        /// <summary>
        /// Sets and gets the Stations property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<Graphic> Stations
        {
            get
            {
                return _stations;
            }

            set
            {

                _stations = value;
                RaisePropertyChanged(StationsPropertyName);
            }
        }
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            this.Quake = new QuakeMessage()
            {
                EventID = "Event Id",
               
                FaultType = "N",
                Flag = "1",
                Magnitude = 5.0,
                Length =0,
                Width =0,
                Lat = 0,
                Lon = 0,
                Strike = 60,
                Depth =3.0,
                Dip= 45,
                Stress =55
               // DeleteMe=false
            };
          
            this.StationData = new StationsInfo()
            {
                PartitionKey = "Grid name",
                CrustalAmplification = "crustal_amps_sample.txt",
                SiteAmplification = "site_amps_d_klimis.txt",
                SoilCondition = "soft-soil",
                KappaCoeffient = 0.044
            };

            if (IsInDesignMode)
            {
                this.StationGroups = new ObservableCollection<string>();
                this.StationGroups.Add("One");
                this.StationGroups.Add("Two");
            }
            else
            {
                this.StationGroups = new ObservableCollection<string>();
                this.Stations = new ObservableCollection<Graphic>(); 
                GetStationsGroups();
               
            }
        }
        public void GetStationsGroups()
        {
            this.IsLoading = true;
            Services.AzureStorageClient client = new AzureStorageClient();
            client.GetStationsGroupsCompleted += (s, a) =>
                {
                    this.StationGroups.Clear();
                    foreach (var item in a.Result )
                    {
                        this.StationGroups.Add(item) ;
                    }
                    this.IsLoading = false;
                };
            client.GetStationsGroupsAsync(); 
            
        }
       
    }
}