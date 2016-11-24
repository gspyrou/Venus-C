using GalaSoft.MvvmLight;
using CloudQuakeClient.CloudQuake;
using System.Collections.ObjectModel;

namespace CloudQuakeClient.ViewModel
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
        /// The <see cref="Quake" /> property's name.
        /// </summary>
        public const string QuakePropertyName = "Quake";

        private QuakeMessage _quake = null;

        /// <summary>
        /// Sets and gets the Quake property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// This property's value is broadcasted by the MessengerInstance when it changes.
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

                var oldValue = _quake;
                _quake = value;
                RaisePropertyChanged(QuakePropertyName); 
            }
        }
        public ObservableCollection<string>  StationPartitions { get; set; }
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                this.Quake = new QuakeMessage();
                this.Quake.EventID = "hjjhhkhk";
                this.StationPartitions = new ObservableCollection<string>();
            }
            else
            {
                // Code runs "for real"
            }

           // GetStationsList(); 
        }
        public void GetStationsList()
        {
            CloudQuake.StorageClient client = new StorageClient();
            client.GetStationPartitionsCompleted += (s, a) =>
                {
                    this.StationPartitions = a.Result ;

                };
        }
    }
}