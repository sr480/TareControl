using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charts.ViewModel
{
    public abstract class BaseAnswerEventArgs : EventArgs
    { }
    public class ConnectionEstablishedEventArgs : BaseAnswerEventArgs
    { }
    public class ConfirmMessageRecievedRecievedEventArgs : BaseAnswerEventArgs
    { }
    public class StepPositionRecievedEventArgs : BaseAnswerEventArgs
    {
        public int StepPosition { get; private set; }
        
        public StepPositionRecievedEventArgs(int stepPosition)
        {
            StepPosition = stepPosition;
        }
    }
    public class SensorValueRecievedEventArgs : BaseAnswerEventArgs
    {
        public int Sensor1 { get; private set; }
        public int Sensor2 { get; private set; }

        public SensorValueRecievedEventArgs(int sensor1, int sensor2)
        {
            Sensor1 = sensor1;
            Sensor2 = sensor2;
        }
    }
    public class AddressRecievedEventArgs : BaseAnswerEventArgs
    {
        public int Address { get; private set; }
        
        public AddressRecievedEventArgs(int address)
        {
            Address = address;
        }
    }
    public class TemperatureRecievedEventArgs : BaseAnswerEventArgs
    {
        public double Temperature { get; private set; }
        public bool SensorNotAvaliable { get; private set; }
        public bool ValueNotReady { get; private set; }

        public TemperatureRecievedEventArgs(int temperature)
        {
            if (temperature == -1)
            {
                Temperature = double.NaN;
                SensorNotAvaliable = true;
            }
            else if (temperature == -2)
            {
                Temperature = double.NaN;
                ValueNotReady = true;
            }
            Temperature = temperature / 16.0;
        }
    }
}
