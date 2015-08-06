using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Timers;
using System.Windows;

namespace Charts.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DBManager _DataBaseManager;
        private readonly StepperController _StepperControl;
        private readonly CalibrationController _CalibrationControl;
        private readonly SensorController _SensorControl;

        private readonly ObservableCollection<string> _AvaliablePorts;
        private string _SelectedPort;
        private string _Status;
        private bool _IsConnected;
        private CommunicationService _CommunicationService;
        
        public CommunicationService CommunicationService
        {
            get
            {
                return _CommunicationService;
            }
        }
        public bool IsConnected
        {
            get { return _IsConnected; }
            private set
            {
                SynchronisationHelper.Synchronise(() =>
                    {
                        if (_IsConnected == value)
                            return;
                        _IsConnected = value;
                        RaisePropertyChanged("IsConnected");
                        StepperControl.EnableControl = IsConnected;
                        CalibrationControl.EnableControl = IsConnected;
                    });
            }
        }
        public string Status
        {
            get { return _Status; }
            private set
            {
                if (_Status == value)
                    return;
                _Status = value;
                RaisePropertyChanged("Status");
            }
        }
        public ObservableCollection<string> AvaliablePorts
        {
            get { return _AvaliablePorts; }
        }
        public string SelectedPort
        {
            get { return _SelectedPort; }
            set
            {
                if (_SelectedPort == value)
                    return;
                _SelectedPort = value;
                RaisePropertyChanged("SelectedPort");
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }
        
        public DBManager DataBaseManager { get { return _DataBaseManager; } }
        public SensorController SensorControl
        {
            get
            {
                return _SensorControl;
            }
        }
        public CalibrationController CalibrationControl
        {
            get
            {
                return _CalibrationControl;
            }
        }
        public StepperController StepperControl { get { return _StepperControl; } }
                
        public Command ConnectCommand { get; private set; }
        
        public MainViewModel()
        {
            _CommunicationService = new CommunicationService();
            _CommunicationService.AnswerRecieved += OnAnswerRecieved;
            _CommunicationService.AnswerError += OnAnswerError;

            Status = "Подключение не установлено";
            //SensorState = "Подключение не установлено";

            _AvaliablePorts = new ObservableCollection<string>(System.IO.Ports.SerialPort.GetPortNames());
            ConnectCommand = new Command((x) => ConnectDisconnect(), (x) => !string.IsNullOrEmpty(SelectedPort) | CommunicationService.IsConnected);

            _DataBaseManager = new DBManager();
            _StepperControl = new StepperController(_CommunicationService);
            _SensorControl = new SensorController(_CommunicationService, _DataBaseManager);
            _CalibrationControl = new CalibrationController(_CommunicationService, _StepperControl, _DataBaseManager, SensorControl);            
        }

        private void ConnectDisconnect()
        {
            if (CommunicationService.IsConnected)
                CommunicationService.Disconnect();
            else
            {
                if (string.IsNullOrEmpty(SelectedPort))
                    throw new Exception("Порт подключения не задан");
                try
                {
                    CommunicationService.Connect(SelectedPort);
                    Status = "Подключение...";
                }
                catch (Exception err)
                {
                    SynchronisationHelper.ShowMessage("Ошибка подключения.\n" + err.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Status = "Ошибка подключения";
                }
            }
        }

        private void ConnectionOpened()
        {
            Status = "Подключение утсановлено";
            IsConnected = true;            
        }
        private void OnAnswerRecieved(object sender, BaseAnswerEventArgs e)
        {
            if (e is ConnectionEstablishedEventArgs)
                ConnectionOpened();
        }
        private void OnAnswerError(object sender, AnswerErrorEventArgs e)
        {
            SynchronisationHelper.ShowMessage("Ошибка связи\n" + e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            switch (e.ErrorType)
            {
                case AnswerErrorType.AnswerParcerError:
                    Status = "Ошибка разбора ответа";
                    break;
                case AnswerErrorType.SynchronizationError:
                    Status = "Ошибка синхронизации";
                    break;
                case AnswerErrorType.TimeoutError:
                    Status = "Ошибка: превышено время ожидания ответа";
                    break;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public void Dispose()
        {
            CalibrationControl.Dispose();
            if (_CommunicationService != null)
                _CommunicationService.Dispose();
        }
        public bool Close()
        {
            if (!_DataBaseManager.IsSaved)
            {
                var result = SynchronisationHelper.ShowMessage("Сохранить изменения?", "Внимание!", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    _DataBaseManager.Save.Execute(null);
                else if(result == MessageBoxResult.Cancel)
                    return false;
            }
            _StepperControl.Dispose();
            _CalibrationControl.Dispose();
            _CommunicationService.Dispose();

            return true;
        }
    }
}
