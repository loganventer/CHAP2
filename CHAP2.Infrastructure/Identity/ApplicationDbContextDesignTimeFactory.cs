using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CHAP2.Infrastructure.Identity;

public class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var path = Environment.GetEnvironmentVariable("CHAP2_DB_PATH") ?? "chap2.design.db";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
