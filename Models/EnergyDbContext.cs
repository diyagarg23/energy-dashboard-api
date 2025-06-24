using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using EnergyDashboardAPI1.Models;

namespace EnergyDashboardAPI1.Models
{
    public partial class EnergyDbContext : DbContext
    {
        public EnergyDbContext() { }

        public EnergyDbContext(DbContextOptions<EnergyDbContext> options)
            : base(options) { }

        public DbSet<Site> Sites { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Meter> Meters { get; set; }
        public DbSet<MeterReading> MeterReadings { get; set; }
        public DbSet<Space> Spaces { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=DESKTOP-9E696H1\\SQLEXPRESS01;Database=EnergyDashboard;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Site
            modelBuilder.Entity<Site>(entity =>
            {
                entity.HasKey(e => e.SiteId).HasName("PK__Sites__B9DCB903D8826677");
                entity.Property(e => e.SiteId).ValueGeneratedNever().HasColumnName("SiteID");
                entity.Property(e => e.SiteName).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.ContactPerson).HasMaxLength(100);
                entity.Property(e => e.ContactEmail).HasMaxLength(100);
                entity.Property(e => e.ContactPhone).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            });

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(100);
            });

            // Meter
            modelBuilder.Entity<Meter>(entity =>
            {
                entity.HasKey(e => e.MeterID).HasName("PK__Meters__59223B8CC59C0DFE");
                entity.Property(e => e.MeterID).HasColumnName("MeterID");
                entity.Property(e => e.MeterName).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.SpaceId).HasColumnName("SpaceID");

                entity.HasOne(d => d.Space)
                      .WithMany(p => p.Meters)
                      .HasForeignKey(d => d.SpaceId)
                      .HasConstraintName("FK__Meters__SpaceID__3A81B327");
            });

            // MeterReading
            modelBuilder.Entity<MeterReading>(entity =>
            {
                entity.HasKey(e => new { e.MeterID, e.ReadingTimestamp });
                entity.Property(e => e.ReadingTimestamp).HasColumnType("datetime");
                entity.Property(e => e.Value).HasColumnName("Value");

                entity.HasOne(e => e.Meter)
                      .WithMany(m => m.MeterReadings)
                      .HasForeignKey(e => e.MeterID)
                      .HasConstraintName("FK_MeterReading_Meter");
            });

            // Space
            modelBuilder.Entity<Space>(entity =>
            {
                entity.HasKey(e => e.SpaceId).HasName("PK__Spaces__83E25E0E28BAD6A3");
                entity.Property(e => e.SpaceId).HasColumnName("SpaceID");
                entity.Property(e => e.ParentSpaceId).HasColumnName("ParentSpaceID");
                entity.Property(e => e.SpaceName).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.SpaceType).HasMaxLength(50).IsUnicode(false);

                entity.HasOne(d => d.ParentSpace)
                      .WithMany(p => p.InverseParentSpace)
                      .HasForeignKey(d => d.ParentSpaceId)
                      .HasConstraintName("FK__Spaces__ParentSp__37A5467C");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
