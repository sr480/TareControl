using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Threading;

namespace Charts.ViewModel
{
    public class CommunicationService : INotifyPropertyChanged, IDisposable
    {
        private bool _TemperatureEnable = true;
        private object locker = new object();

        private const string CONNECT_COMMAND = "CONNECT";       //Запрос подключения
        private const int ANSWER_TIMEOUT = 3000;

        private const string STATUSSTEP_COMMAND = "STATUS STEP";    //Запрос положения двигателя
        private const string STATUS_COMMAND = "STATUS";    //Запрос измерения
        private const string SETHOME_COMMAND = "SETHOME";       //Установить нулевое положение
        private const string STEPUP_COMMAND = "STEPUP";         //Шагать вверх на заданное кол-во шагов
        private const string STEPDN_COMMAND = "STEPDN";         //Шагать вниз на заданное кол-во шагов
        private const string ADDRESS_COMMAND = "ADRES";         //запрос адреса
        private const string STOP_TERMO_COMMAND = "STOP_T";     //отключение цикла опроса температуры
        private const string START_TERMO_COMMAND = "START_T";   //включение цикла опроса температуры
        private const string TERMO_COMMAND = "TEMPERATURE"; //опрос термодатчика
        private const string ANSWERPARCE_EXPR = @"^<(?<NUM>\d+):(?<BODY>.*)>$";
        private const string BODYPARCE_EXPR = @"^((?<ARG>\S+)(\s|$))+$";

        private int messageId;
        private string lastMessage;
        //private int? lastSensorId;
        private volatile bool answerRecieved;
        private volatile bool isConnected;
        private System.Timers.Timer answerTimeout;
        private System.IO.Ports.SerialPort port;
        private readonly Regex answerParceRegX;
        private readonly Regex bodyParceRegX;

        private readonly ObservableCollection<string> log;
        public ObservableCollection<string> Log { get { return log; } }
        public bool IsAnswerRecieved
        {
            get
            {
                return answerRecieved;
            }
            set
            {
                lock (locker)
                {
                    answerRecieved = value;
                    RaisePropertyChanged("IsAnswerRecieved");
                    RaisePropertyChanged("Ready");
                }
            }
        }
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                lock (locker)
                {
                    isConnected = value;
                    RaisePropertyChanged("IsConnected");
                    RaisePropertyChanged("Ready");
                }
            }
        }
        public bool TemperatureEnable
        {
            get
            {
                return _TemperatureEnable;
            }
            private set
            {
                _TemperatureEnable = value;
            }
        }
        public bool Ready
        {
            get
            {
                return IsConnected & IsAnswerRecieved;
            }
        }
        public CommunicationService()
        {
            answerParceRegX = new Regex(ANSWERPARCE_EXPR, RegexOptions.Compiled);
            bodyParceRegX = new Regex(BODYPARCE_EXPR, RegexOptions.Compiled);
            answerTimeout = new System.Timers.Timer(ANSWER_TIMEOUT);
            answerTimeout.Elapsed += OnTimeoutElapsed;

            log = new ObservableCollection<string>();
        }

        public void Connect(string portName)
        {
            messageId = 0;
            ClosePort();
            OpenPort(portName);

            messageId++;
            lastMessage = CONNECT_COMMAND;
            IsAnswerRecieved = false;

            string str = FormatMessage(CONNECT_COMMAND, null);
            SynchronisationHelper.Synchronise(() => Log.Insert(0, str));
            WriteToPort(str);

            answerTimeout.Start();
        }
        public void Disconnect()
        {
            ClosePort();
        }
        public bool RequestPosition()
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(STATUSSTEP_COMMAND, null);
                return true;
            }
        }
        public bool RequestStatus(int sensorId)
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(STATUS_COMMAND, sensorId.ToString());
                return true;
            }

        }
        public bool RequestAddress(int sensorId)
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(ADDRESS_COMMAND, sensorId.ToString());
                return true;
            }

        }
        public bool RequestSetHome()
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(SETHOME_COMMAND, null);
                return true;
            }

        }
        public bool RequestStepUp(int steps)
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(STEPUP_COMMAND, steps.ToString()); return true;
            }

        }
        public bool RequestStepDown(int steps)
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(STEPDN_COMMAND, steps.ToString());
                return true;
            }

        }
        public bool RequestTermoSensorStop()
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(STOP_TERMO_COMMAND, null);
                TemperatureEnable = false;
                return true;
            }

        }
        public bool RequestTermoSensorStart()
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(START_TERMO_COMMAND, null);
                TemperatureEnable = true;
                return true;
            }
        }
        public bool RequestTermo(int sensorId)
        {
            lock (locker)
            {
                if (!Ready)
                    return false;
                SendCommand(TERMO_COMMAND, sensorId.ToString());
                return true;
            }
        }
        public event EventHandler<BaseAnswerEventArgs> AnswerRecieved;
        public event EventHandler<AnswerErrorEventArgs> AnswerError;
        public void Dispose()
        {
            answerTimeout.Dispose();
            if (port != null)
                port.Dispose();
        }

        private void ClosePort()
        {
            if (port == null)
                return;
            if (port.IsOpen)
                port.Close();
            port.DataReceived -= OnDataReceived;
            port.Dispose();
            port = null;
            IsConnected = false;
        }
        private void OpenPort(string portName)
        {
            port = new System.IO.Ports.SerialPort(portName, 115200);
            port.DataReceived += OnDataReceived;
            try
            {
                port.Open();
                Thread.Sleep(2000);
            }
            catch (Exception err)
            {
                throw new Exception("Error on opening port", err);
            }
        }
        private void CheckCommunication()
        {
            lock (locker)
            {
                if (!IsConnected)
                    throw new Exception("Connection closed");

                if (!IsAnswerRecieved)
                    throw new Exception("Last message was not recieved");
            }
        }
        private void SendCommand(string message, string argument)
        {
            CheckCommunication();
            messageId++;
            lastMessage = message;
            lock (locker)
            {
                IsAnswerRecieved = false;

                string str = FormatMessage(message, argument);
                SynchronisationHelper.Synchronise(() => Log.Insert(0, str));
                WriteToPort(str);

                answerTimeout.Start();
            }
        }
        private string FormatMessage(string message, string argument)
        {
            if (string.IsNullOrEmpty(argument))
                return string.Format("<{0}:{1}>", messageId, message);
            return string.Format("<{0}:{1} {2}>", messageId, message, argument);
        }
        private void WriteToPort(string message)
        {
            if (port == null || !port.IsOpen)
                throw new Exception("Port is not opened");
            try
            {
                byte[] bytesToWrite = Encoding.ASCII.GetBytes(message);
                port.Write(bytesToWrite, 0, bytesToWrite.Length);
            }
            catch
            { }
        }
        private void OnDataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string result = string.Empty;
            while (!result.EndsWith(">") & result.Length < 128)
                result += port.ReadExisting();

            IsAnswerRecieved = true;
            answerTimeout.Stop();

            SynchronisationHelper.Synchronise(() => Log.Insert(0, result));
            if (System.Diagnostics.Debugger.IsAttached)
                Console.WriteLine(result);

            string body = CheckAnswer_ReadBody(result);
            if (body != null)
                ParceBody(body);
        }
        private string CheckAnswer_ReadBody(string answer)
        {
            var match = answerParceRegX.Match(answer);

            if (!match.Success)
            {
                RaiseAnswerParcerError(answer);
                return null;
            }
            if (messageId != Convert.ToInt32(match.Groups["NUM"].Value))
            {
                RaiseSyncError();
                return null;
            }
            return match.Groups["BODY"].Value;
        }
        private void ParceBody(string body)
        {
            if (body == "OK" &
                (lastMessage == CONNECT_COMMAND |
                lastMessage == SETHOME_COMMAND |
                lastMessage == STEPUP_COMMAND |
                lastMessage == STEPDN_COMMAND |
                lastMessage == STOP_TERMO_COMMAND |
                lastMessage == START_TERMO_COMMAND))
            {
                if (lastMessage == CONNECT_COMMAND)
                {
                    IsConnected = true;
                    RaiseAnswerRecieved(new ConnectionEstablishedEventArgs());
                }
                else
                    RaiseAnswerRecieved(new ConfirmMessageRecievedRecievedEventArgs());
            }
            else
            {
                var match = bodyParceRegX.Match(body);
                if (!match.Success)
                {
                    RaiseAnswerParcerError(body);
                    return;
                }
                ReadArguments(match.Groups["ARG"].Captures.Cast<Capture>().Select(c => c.Value).ToArray());
            }
        }
        private void ReadArguments(string[] args)
        {
            if (args.Length == 2 & lastMessage == STATUS_COMMAND)
                RaiseAnswerRecieved(new SensorValueRecievedEventArgs(int.Parse(args[0]), int.Parse(args[1])));
            else if (args.Length == 1 & lastMessage == STATUSSTEP_COMMAND)
                RaiseAnswerRecieved(new StepPositionRecievedEventArgs(int.Parse(args[0])));
            else if (args.Length == 1 & lastMessage == ADDRESS_COMMAND)
                RaiseAnswerRecieved(new AddressRecievedEventArgs(int.Parse(args[0])));
            else if (args.Length == 1 & lastMessage == TERMO_COMMAND)
                RaiseAnswerRecieved(new TemperatureRecievedEventArgs(int.Parse(args[0])));
            else
                RaiseAnswerParcerError(string.Empty);
        }
        private void OnTimeoutElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            answerTimeout.Stop();
            if (IsAnswerRecieved)
                return;
            RaiseAnswerTimeoutError();
        }
        private void RaiseAnswerTimeoutError()
        {
            if (AnswerError != null)
            {
                AnswerError(this, new AnswerErrorEventArgs(AnswerErrorType.TimeoutError,
                    "Last command finished with timeout error", messageId));
            }
        }
        private void RaiseAnswerParcerError(string answer)
        {
            if (AnswerError != null)
            {
                AnswerError(this, new AnswerErrorEventArgs(AnswerErrorType.AnswerParcerError,
                    "Last command finished with parcer error: " + answer, messageId));
            }
        }
        private void RaiseSyncError()
        {
            if (AnswerError != null)
            {
                AnswerError(this, new AnswerErrorEventArgs(AnswerErrorType.SynchronizationError,
                    "Last command finished with synchronisation error", messageId));
            }
        }
        private void RaiseAnswerRecieved(BaseAnswerEventArgs eventArgs)
        {
            if (AnswerRecieved != null)
                AnswerRecieved(this, eventArgs);
        }

        public void WaitServiceReady(int millis)
        {
            DateTime timeStamp = DateTime.Now;
            while (!Ready)
                if ((DateTime.Now - timeStamp).Milliseconds > millis)
                    throw new Exception("Превышено время ожидания подтверждения");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
