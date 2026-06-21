using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Models;

namespace Rastreador.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Geofence> Geofences => Set<Geofence>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.Plate)
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.Imei)
            .IsUnique();

        modelBuilder.Entity<Position>()
            .HasOne(p => p.Vehicle)
            .WithMany(v => v.Positions)
            .HasForeignKey(p => p.VehicleId);

        modelBuilder.Entity<Position>()
            .HasIndex(p => new { p.VehicleId, p.Timestamp });

        modelBuilder.Entity<Geofence>()
            .HasOne(g => g.Vehicle)
            .WithMany(v => v.Geofences)
            .HasForeignKey(g => g.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Geofence>()
            .Property(g => g.Area)
            .HasColumnType("geometry(Polygon, 4326)");

        modelBuilder.Entity<Alert>()
            .HasOne(a => a.Vehicle)
            .WithMany()
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Alert>()
            .HasIndex(a => new { a.VehicleId, a.Timestamp });
    }
}
