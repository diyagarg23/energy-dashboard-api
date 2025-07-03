using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnergyDashboardAPI1.Models
{
    public class Site
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int SiteId { get; set; }

        [Required]
        public string SiteName { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string ContactPerson { get; set; }

        [Required]
        public string ContactEmail { get; set; }

        [Required]
        public string ContactPhone { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
