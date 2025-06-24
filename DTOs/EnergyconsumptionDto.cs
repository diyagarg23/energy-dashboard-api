namespace EnergyDashboardAPI1.DTOs
{
    namespace EnergyDashboardAPI1.DTOs
    {
        public class EnergyConsumptionDto
        {
            public int MeterID { get; set; }
            public DateTime ReadingTimestamp { get; set; }
            public decimal EnergyConsumed { get; set; }
        }
    }
}
