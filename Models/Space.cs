using System;
using System.Collections.Generic;

namespace EnergyDashboardAPI1.Models;

public partial class Space
{
    public int SpaceId { get; set; }

    public int? ParentSpaceId { get; set; }

    public string? SpaceName { get; set; }

    public string? SpaceType { get; set; }

    public virtual ICollection<Space> InverseParentSpace { get; set; } = new List<Space>();

    public virtual ICollection<Meter> Meters { get; set; } = new List<Meter>();

    public virtual Space? ParentSpace { get; set; }
}
