using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Timers;

namespace Charts.ViewModel
{
    public class SensorController : INotifyPropertyChanged
    {
        private const string DATA_NOT_READY = "Данные не готовы";
        private const string SENSOR_NOT_AVALIABLE = "Датчик не доступен";
        private const int REQUEST_TIMEOUT = 3000;
        private string _Temperature;
        private string _Sensor1;
        private string _Sensor2;
        private Timer _RequestTimer;
        private readonly CommunicationService _CommunicationService;
        private readonly DBManager _DataBaseManager;

        private SensorInfo _SelectedSensor;
        private int? _SelectedSensorNumber;
        private readonly int[] sensorNumbers = { 0, 1, 2, 3, 4, 5, 6 };

        public int[] SensorNumbers { get { return sensorNumbers; } }
        public int? SelectedSensorNumber
        {
            get
            {
                return _SelectedSensorNumber;
            }
            set
            {
                if (_SelectedSensorNumber == value)
                    return;
                _SelectedSensorNumber = value;
                RaisePropertyChanged("SelectedSensorNumber");
                SelectedSensor = null;
                GetAddress();
            }
        }
        public SensorInfo SelectedSensor
        {
            get { return _SelectedSensor; }
            set
            {
                if (_SelectedSensor == value)
                    return;
                _SelectedSensor = value;
                if (_SelectedSensor == null)
                {
                    Sensor1 = SENSOR_NOT_AVALIABLE;
                    Sensor2 = SENSOR_NOT_AVALIABLE;
                    Temperature = SENSOR_NOT_AVALIABLE;
                }
                else
                {
                    Sensor1 = DATA_NOT_READY;
                    Sensor2 = DATA_NOT_READY;
                    Temperature = DATA_NOT_READY;
                }
                RaisePropertyChanged("SelectedSensor");
            }
        }

        public string Sensor1
        {
            get
            {
                return _Sensor1;
            }
            set
            {
                if (_Sensor1 == value)
                    return;
                _Sensor1 = value;
                RaisePropertyChanged("Sensor1");
            }
        }
        public string Sensor2
        {
            get
            {
                return _Sensor2;
            }
            set
            {
                if (_Sensor2 == value)
                    return;
                _Sensor2 = value;
                RaisePropertyChanged("Sensor2");
            }
        }
        public string Temperature
        {
            get
            {
                return _Temperature;
            }
            set
            {
                if (_Temperature == value)
                    return;
                _Temperature = value;
                RaisePropertyChanged("Temperature");
            }
        }
        public Command RequestSensorCommand
        {
            get;
            private set;
        }
        public SensorController(CommunicationService communicationService, DBManager dataBaseManager)
        {
            if (communicationService == null)
                throw new ArgumentNullException("communicationService", "communicationService is null.");
            if (dataBaseManager == null)
                throw new ArgumentNullException("dataBaseManager", "dataBaseManager is null.");

            _DataBaseManager = dataBaseManager;
            _CommunicationService = communicationService;
            _CommunicationService.AnswerRecieved += OnAnswerRecieved;
            _Sensor1 = DATA_NOT_READY;
            _Sensor2 = DATA_NOT_READY;
            _Temperature = DATA_NOT_READY;
            RequestSensorCommand = new Command((x) => RequestSensor(), (x) => true);
            //_RequestTimer = new Timer(REQUEST_TIMEOUT);
            //_RequestTimer.Elapsed += new ElapsedEventHandler(RequestTimer_Elapsed);
            //_RequestTimer.Start();
        }
        
        private void OnAnswerRecieved(object sender, BaseAnswerEventArgs e)
        {
            if (e is SensorValueRecievedEventArgs)
            {
                var valueInfo = (SensorValueRecievedEventArgs)e;

                if (valueInfo.Sensor1 == -1)
                {
                    SynchronisationHelper.Synchronise(() => Sensor1 = SENSOR_NOT_AVALIABLE);
                    SynchronisationHelper.Synchronise(() => Sensor2 = SENSOR_NOT_AVALIABLE);
                }
                else
                {
                    SynchronisationHelper.Synchronise(() => Sensor1 = valueInfo.Sensor1.ToString("F1"));
                    SynchronisationHelper.Synchronise(() => Sensor2 = valueInfo.Sensor2.ToString("F1"));
                }
                if (SensorValueRecieved != null)
                    SensorValueRecieved(this, valueInfo);
            }
            if (e is TemperatureRecievedEventArgs)
            {
                TemperatureRecievedEventArgs temperatureRecievedEventArgs = (TemperatureRecievedEventArgs)e;
                if (temperatureRecievedEventArgs.SensorNotAvaliable)
                    SynchronisationHelper.Synchronise(() => Temperature = SENSOR_NOT_AVALIABLE);
                else if (temperatureRecievedEventArgs.ValueNotReady)
                    SynchronisationHelper.Synchronise(() => Temperature = DATA_NOT_READY);
                else
                    SynchronisationHelper.Synchronise(() => Temperature = temperatureRecievedEventArgs.Temperature.ToString("F1"));
            }
            else if (e is AddressRecievedEventArgs)
            {
                var addressInfo = (AddressRecievedEventArgs)e;
                if (addressInfo.Address == -1)
                {
                    SynchronisationHelper.Synchronise(() => SelectedSensor = null);
                    SynchronisationHelper.ShowMessage("На выбраном канале датчик отсутствует", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                    SynchronisationHelper.Synchronise(() =>
                        SelectedSensor = _DataBaseManager.GetSensor(SelectedSensorNumber.Value, addressInfo.Address));
            }
        }
        void RequestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RequestSensor();
        }

        private void RequestSensor()
        {
            System.Threading.Thread requestThread = new System.Threading.Thread(ThreadAction);
            requestThread.Start();
        }

        private void ThreadAction()
        {
            if (!_CommunicationService.Ready)
                return;
            if (!SelectedSensorNumber.HasValue)
                return;
            if (SelectedSensor == null)
                return;

            if (_CommunicationService.TemperatureEnable)
                while (!_CommunicationService.RequestTermo(SelectedSensorNumber.Value)) ;
            System.Threading.Thread.Sleep(500);
            while (!_CommunicationService.RequestStatus(SelectedSensorNumber.Value)) ;
        }
        public void RequestSensorValue()
        {
            _CommunicationService.RequestStatus(_SelectedSensorNumber.Value);
        }
        private void GetAddress()
        {
            if (!_CommunicationService.Ready)
            {
                SelectedSensor = null;
                return;
            }
            _CommunicationService.RequestAddress(SelectedSensorNumber.Value);
        }
        public event EventHandler<SensorValueRecievedEventArgs> SensorValueRecieved;
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
