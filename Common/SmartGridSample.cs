using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SmartGridSample
    {
        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public double Voltage { get; set; }

        [DataMember]
        public double Current { get; set; }

        [DataMember]
        public bool FaultIndicator { get; set; }

        [DataMember]
        public double PowerUsage { get; set; }

        [DataMember]
        public double Frequency { get; set; }
    }
}
