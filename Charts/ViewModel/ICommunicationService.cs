using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charts.ViewModel
{
    public interface ICommunicationService
    {
        void Connect(string port);
        void RequestStatus(int sensorId);
        void RequestSetHome();
        void RequestStepUp(int steps);
        void RequestStepDown(int steps);
        event EventHandler<BaseAnswerEventArgs> AnswerRecieved;
        event EventHandler<AnswerErrorEventArgs> AnswerError;
    }
}
