namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;

public class DataContext : DbContext
{
#pragma warning disable CS8618
    public DbSet<AudioModel> AudioModels { get; set; }
#pragma warning restore CS8618

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseSqlServer(@"Data Source=.;Initial Catalog=listentome;Integrated Security=True");
    }
}