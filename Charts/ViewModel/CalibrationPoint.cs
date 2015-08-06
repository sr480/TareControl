using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charts.ViewModel
{
    [Serializable()]
    public class CalibrationPoint
    {
        public double Position { get; set; }
        public int Sensor1 { get; set; }
        public int Sensor2 { get; set; }
    }
}
