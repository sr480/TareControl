using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChartControl
{
    class RawValueInfo
    {
        public double Data { get; private set; }
        public double Value { get; private set; }
        public ValueMemberDefinition ValueMember { get; private set; }
        public RawValueInfo(double data, double value, ValueMemberDefinition definition)
        {
            if (definition == null)
                throw new Exception("definition can't be null");
            Data = data;
            Value = value;
            ValueMember = definition;
        }
    }
}
