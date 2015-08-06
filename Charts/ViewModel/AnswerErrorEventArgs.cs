using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charts.ViewModel
{
    public class AnswerErrorEventArgs : EventArgs
    {
        public AnswerErrorType ErrorType { get; private set; }
        public string Message { get; private set; }
        public int RequestNumber { get; private set; }
        public AnswerErrorEventArgs(AnswerErrorType errorType, string message, int reqNum)
        {
            ErrorType = errorType;
            Message = message;
            RequestNumber = reqNum;
        }
    }
}
