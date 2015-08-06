using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Timers;

namespace Charts.ViewModel
{
    public class StepperController : INotifyPropertyChanged, IDisposable
    {
        private bool _EnableControl;
        private const int STEPS_PER_MM = 200;
        private CommunicationService _CommunicationService;
        private readonly double[] _AvaliableSteps = new double[] { 0.25, 0.5, 1, 2, 5, 10, 20 };
        private double _SelectedStepRate;
        private Timer _requestTimer;
        private int _DestinationPosition;
        private int _Position;

        public IEnumerable<double> AvaliableSteps
        {
            get { return _AvaliableSteps; }
        }
        public double SelectedStepRate
        {
            get { return _SelectedStepRate; }
            set
            {
                if (_SelectedStepRate == value)
                    return;
                _SelectedStepRate = value;
                RaisePropertyChanged("SelectedStepRate");
            }
        }
        public int Position
        {
            get { return _Position; }
            set
            {
                if (_Position == value)
                    return;
                _Position = value;
                RaisePropertyChanged("Position");
                RaisePropertyChanged("PositionMM");
                RaisePropertyChanged("Ready");
                UpdateCommands();
            }
        }
        public int DestinationPosition
        {
            get { return _DestinationPosition; }
            set
            {
                if (_DestinationPosition == value)
                    return;
                _DestinationPosition = value;
                RaisePropertyChanged("DestinationPosition");
                UpdateCommands();
            }
        }
        public double PositionMM
        {
            get { return (double)Position / (double)STEPS_PER_MM; }
        }
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
                //SetHome();
            }
        }
        public bool Ready
        {
            get { return /*_CommunicationService.Ready &*/ EnableControl & DestinationPosition == Position; }
        }
        
        public Command StepUpCommand { get; private set; }
        public Command StepDownCommand { get; private set; }
        public Command SetHomeCommand { get; private set; }
        public Command GoHomeCommand { get; private set; }

        public StepperController(CommunicationService communicationService)
        {
            if (communicationService == null)
                throw new ArgumentNullException("communicationService", "communicationService is null.");
            _CommunicationService = communicationService;
            _CommunicationService.AnswerRecieved += OnAnswerRecieved;
            SelectedStepRate = AvaliableSteps.FirstOrDefault();
            StepDownCommand = new Command((x) => MakeMove(-SelectedStepRate), (x) => Ready);
            StepUpCommand = new Command((x) =>  MakeMove(SelectedStepRate), (x) => Ready);
            SetHomeCommand = new Command((x) => SetHome(), (x) => Ready);
            GoHomeCommand = new Command((x) => GoHome(), (x) => Ready);

            _requestTimer = new Timer(500);
            _requestTimer.Elapsed += OnRequestTimerElapsed;
            _requestTimer.Start();
        }
        public void SetHome()
        {
            if (!_CommunicationService.Ready)
                throw new Exception("Коммуникационный сервер не готов к передаче");
            _CommunicationService.RequestSetHome();
            DestinationPosition = Position = 0;
        }
        public bool MakeMove(double length)
        {
            int steps = (int)(length * STEPS_PER_MM);
            if (MakeStep(steps))
            {
                DestinationPosition += steps;
                return true;
            }
            return false;
        }
        private void GoHome()
        {
            MakeStep(-Position);
            DestinationPosition = 0;
        }
        private bool MakeStep(int steps)
        {
            if (steps < 0)
                return _CommunicationService.RequestStepDown(Math.Abs(steps));
            else
                return _CommunicationService.RequestStepUp(Math.Abs(steps));
        }

        private void OnRequestTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Position != DestinationPosition)
                _CommunicationService.RequestPosition();
        }

        private void OnAnswerRecieved(object sender, BaseAnswerEventArgs e)
        {
            if (e is StepPositionRecievedEventArgs)
                Position = ((StepPositionRecievedEventArgs)e).StepPosition;
            else if (e is ConnectionEstablishedEventArgs)
                SetHome();
        }
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        private void UpdateCommands()
        {
            SynchronisationHelper.Synchronise(() =>
            {
                StepUpCommand.RaiseCanExecuteChanged();
                StepDownCommand.RaiseCanExecuteChanged();
                SetHomeCommand.RaiseCanExecuteChanged();
                GoHomeCommand.RaiseCanExecuteChanged();
            });
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            _requestTimer.Dispose();            
        }
    }
}
