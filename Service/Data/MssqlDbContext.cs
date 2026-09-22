using Microsoft.EntityFrameworkCore;

public class MssqlDbContext : DbContext
{
    public MssqlDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // MSSQL - WindTurbineMetric
    public DbSet<WindTurbineMetric> WindTurbineMetric { get; set; }
}