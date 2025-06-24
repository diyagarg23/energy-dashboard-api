using System;
using System.Collections.Generic;

namespace EnergyDashboardAPI1.Models;

public partial class Meter
{
    public int MeterID { get; set; }

    public int SpaceId { get; set; }

    public string? MeterName { get; set; }

    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

    public virtual Space Space { get; set; }
}
