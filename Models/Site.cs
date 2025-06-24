using System;
using System.Collections.Generic;

namespace EnergyDashboardAPI1.Models;

public partial class Site
{
    public int SiteId { get; set; }

    public string SiteName { get; set; } = null!;

    public string? Address { get; set; }

    public string? ContactPerson { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public DateTime? CreatedAt { get; set; }

}
