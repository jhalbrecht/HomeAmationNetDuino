using System;
using Microsoft.SPOT;

namespace HaNdP1
{
    public class SummaryTemperatureData
    {
        public SummaryTemperatureData()
        {
        }

        public SummaryTemperatureData(String deviceName, DateTime currentTime, float t0, float t1)
        {
            this.DataLoggerDeviceName = deviceName;
            this.CurrentMeasuredTime = currentTime;
            this.CurrentTemperature0 = t0;
            this.CurrentTemperature1 = t1;
        }

        public string DataLoggerDeviceName { get; set; } // jha 2/14/2012

        // [DataMember]
        public float RecordedMinimumTemperature0 { get; set; }
        // [DataMember]
        public DateTime RecordedMinimumTime { get; set; }
        // [DataMember]
        public float RecordedMaximumTemperature0 { get; set; }
        // [DataMember]
        public DateTime RecordedMaximumTime { get; set; }
        //[DataMember]
        public float RecordedAverageTemperature0 { get; set; }
        //[DataMember]
        public float ThirtyDayMinTemperature0 { get; set; }
        //[DataMember]
        public DateTime ThirtyDayMinTime { get; set; }
        //[DataMember]
        public float ThirtyDayMaxTemperature0 { get; set; }
        //[DataMember]
        public DateTime ThirtyDayMaxTime { get; set; }
        //[DataMember]
        public float ThirtyDayAvgTemperature0 { get; set; }
        //[DataMember]
        public float CurrentTemperature0 { get; set; }
        //[DataMember]
        public float CurrentTemperature1 { get; set; }
        //[DataMember]
        public DateTime CurrentMeasuredTime { get; set; }
        //
        //public string thirtyDayMaxTemperature0long { get { return thirtyDayMaxTemperature0.ToString("{c}"); } }
    }
}
