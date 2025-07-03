namespace EnergyDashboardAPI1.Models
{
    public class SiteCreateDto
    {
        public string SiteName { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
