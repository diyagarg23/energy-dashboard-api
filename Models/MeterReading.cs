using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnergyDashboardAPI1.Models
{
    public class MeterReading
    {
        public int MeterID { get; set; }
        public DateTime ReadingTimestamp { get; set; }

        public decimal Value { get; set; }

        public Meter Meter { get; set; }  
    }
}