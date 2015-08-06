using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace Charts.ViewModel
{
    public class CalibrationController : INotifyPropertyChanged, IDisposable
    {
        private readonly DBManager _DataBaseManager;
        private bool _EnableControl = false;
        private int _MeasureDelay = DELAY_MEASURE_MS;
        const double CALIBRATIONSTEP = 0.2;
        const int CALIBRATIONPOINTCOUNT = 200;
        private readonly CommunicationService _CommunicationService;
        const int DELAY_MEASURE_MS = 3000;

        private readonly StepperController _StepperControl;
        private readonly SensorController _SensorControl;

        private int _CalibrationPointCount = CALIBRATIONPOINTCOUNT;
        private double _CalibrationStep = CALIBRATIONSTEP;

        private volatile bool _CalibrationInProgress = false;
        private volatile bool _waitingForValue = false;

        private int _CalibrationLength;
        private int _CalibrationProgress;

        private Thread _CalibrationThread;
        public bool EnableControl
        {
            get { return _EnableControl; }
            set
            {
                if (_EnableControl == value)
                    return;
                _EnableControl = value;
                RaisePropertyChanged("EnableControl");
                RaisePropertyChanged("Ready");
                UpdateCommands();                
            }
        }
        public double CalibrationStep
        {
            get { return _CalibrationStep; }
            set
            {
                if (_CalibrationStep == value)
                    return;
                _CalibrationStep = value;
                RaisePropertyChanged("CalibrationStep");
            }
        }
        public int CalibrationPointCount
        {
            get { return _CalibrationPointCount; }
            set
            {
                if (_CalibrationPointCount == value)
                    return;
                _CalibrationPointCount = value;
                RaisePropertyChanged("CalibrationPointCount");
            }
        }

        public int CalibrationLength
        {
            get { return _CalibrationLength; }
            set
            {
                if (_CalibrationLength == value)
                    return;
                _CalibrationLength = value;
                RaisePropertyChanged("CalibrationLength");
            }
        }
        public int MeasureDelay
        {
            get { return _MeasureDelay; }
            set
            {
                if (_MeasureDelay == value)
                    return;
                _MeasureDelay = value;
                RaisePropertyChanged("MeasureDelay");
            }
        }
        public int CalibrationProgress
        {
            get { return _CalibrationProgress; }
            set
            {
                if (_CalibrationProgress == value)
                    return;
                _CalibrationProgress = value;
                RaisePropertyChanged("CalibrationProgress");
            }
        }
        public bool CalibrationInProgress
        {
            get { return _CalibrationInProgress; }
            set
            {
                if (_CalibrationInProgress == value)
                    return;
                _CalibrationInProgress = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("CalibrationInProgress"));
                _StepperControl.EnableControl = !_CalibrationInProgress;
            }
        }

        private void UpdateCommands()
        {
            StartStopCalibrationCommand.RaiseCanExecuteChanged();
        }
        public Command StartStopCalibrationCommand { get; private set; }

        public CalibrationController(CommunicationService communicationService, StepperController stepperControl, DBManager dBManager, SensorController sensorControl)
        {
            if (communicationService == null)
                throw new ArgumentNullException("communicationService", "communicationService is null.");
            if (sensorControl == null)
                throw new ArgumentNullException("sensorControl", "sensorControl is null.");
            if (stepperControl == null)
                throw new ArgumentNullException("stepperControl", "stepperControl is null.");
            if (dBManager == null)
                throw new ArgumentNullException("dBManager", "dBManager is null.");

            _CommunicationService = communicationService;
            _StepperControl = stepperControl;
            _DataBaseManager = dBManager;
            _SensorControl = sensorControl;

            StartStopCalibrationCommand = new Command((x) => StartStopCalibaration(), (x) => EnableControl);
            _SensorControl.SensorValueRecieved += OnSensorValueRecieved;
        }

        private void StartStopCalibaration()
        {
            if (CalibrationInProgress)
            {
                if (SynchronisationHelper.ShowMessage("Прервать тарировку?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                CalibrationInProgress = false;
                _CalibrationThread.Join();
                _CalibrationThread = null;
                return;
            }

            if (!_CommunicationService.Ready)
            {
                SynchronisationHelper.ShowMessage("Подключение не установлено - тарировка невозможна", "Внимание", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (_SensorControl.SelectedSensor == null & !CalibrationInProgress)
            {
                SynchronisationHelper.ShowMessage("Датчик не подключен, тарировка не возможна", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (_SensorControl.SelectedSensor.Points.Count > 0 & !CalibrationInProgress)
            {
                if (SynchronisationHelper.ShowMessage("В базе данных уже существуют данные о точках тарировки,\n повторная тарировка приведет к потере данных.\n Продолжить?", "Вниамние!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    _DataBaseManager.ClearPoints(_SensorControl.SelectedSensor.SensorNumber, _SensorControl.SelectedSensor.SensorAddress);
                else
                    return;
            }
            if (SynchronisationHelper.ShowMessage("Магнит установлен в исходном положении?", "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                SynchronisationHelper.ShowMessage("Установите же!", "Чего вы ждете?", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            CalibrationInProgress = true;
            _CalibrationThread = new Thread(new ThreadStart(Calibrate));
            _CalibrationThread.Name = "CalibrationThread";
            _CalibrationThread.Start();

        }
        private void PrepareCalibration()
        {
            while (!_CommunicationService.Ready) ;
            _CommunicationService.RequestTermoSensorStop();
            Thread.Sleep(1000);
            _StepperControl.SetHome();
            Thread.Sleep(1000);

        }
        private void Calibrate()
        {
            try
            {
                PrepareCalibration();
                _waitingForValue = false;
                for (int i = 0; i <= CalibrationPointCount; i++)
                {
                    //var timeStamp = DateTime.Now;
                    
                    //while ((DateTime.Now - timeStamp).TotalMilliseconds < DELAY_MEASURE_MS) ;
                    SynchronisationHelper.Synchronise(() => CalibrationProgress = i);

                    if (!CalibrationInProgress) break;

                    Thread.Sleep(MeasureDelay);
                    
                    _waitingForValue = true;
                    _SensorControl.RequestSensorValue();
                    Thread.Sleep(100);

                    while (_waitingForValue)
                        if (!CalibrationInProgress) break;
                    
                    if (i == CalibrationPointCount)
                        break;

                    while (!_StepperControl.MakeMove(-CalibrationStep)) ;
                    
                    while (_StepperControl.DestinationPosition != _StepperControl.Position)
                        if (!CalibrationInProgress) break;
                }
                _CommunicationService.RequestTermoSensorStart();
                SynchronisationHelper.Synchronise(() => CalibrationProgress = 0);
                if (CalibrationInProgress)
                {
                    SynchronisationHelper.Synchronise(() => CalibrationInProgress = false);
                    SynchronisationHelper.ShowMessage("Тарировка успешно завершена!", "Тарировка завершена", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception err)
            {
                CalibrationInProgress = false;
                SynchronisationHelper.ShowMessage("В процессе тарировки возникла ошибка, процесс был прерван.\n" + err.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);                
            }
        }
        private void OnSensorValueRecieved(object sender, SensorValueRecievedEventArgs e)
        {
            if (!_waitingForValue)
                return;
            _waitingForValue = false;
            SynchronisationHelper.Synchronise(() =>
                    _DataBaseManager.AddValue(_SensorControl.SelectedSensor.SensorNumber,
                    _SensorControl.SelectedSensor.SensorAddress,
                    new CalibrationPoint() { Position = _StepperControl.PositionMM, Sensor1 = e.Sensor1, Sensor2 = e.Sensor2 }));            
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        //private void WriteLog(string text)
        //{
        //    SynchronisationHelper.Synchronise(() => _CommunicationService.Log.Insert(0, text));
        //}
        public void Dispose()
        {
            CalibrationInProgress = false;
            if (_CalibrationThread != null)
                _CalibrationThread.Join();
        }
    }
}
