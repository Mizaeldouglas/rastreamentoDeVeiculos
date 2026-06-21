using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Models;

namespace Rastreador.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Geofence> Geofences => Set<Geofence>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Company)
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Company)
            .WithMany()
            .HasForeignKey(v => v.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Placa única por empresa (não mais globalmente única, já que cada empresa tem sua própria frota)
        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => new { v.CompanyId, v.Plate })
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
            .HasOne(g => g.Company)
            .WithMany()
            .HasForeignKey(g => g.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Geofence>()
            .HasOne(g => g.Vehicle)
            .WithMany(v => v.Geofences)
            .HasForeignKey(g => g.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Geofence>()
            .Property(g => g.Area)
            .HasColumnType("geometry(Polygon, 4326)");

        modelBuilder.Entity<Alert>()
            .HasOne(a => a.Company)
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Alert>()
            .HasOne(a => a.Vehicle)
            .WithMany()
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Alert>()
            .HasIndex(a => new { a.VehicleId, a.Timestamp });
    }
}
