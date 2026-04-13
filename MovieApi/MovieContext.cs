using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

public class MovieContext : DbContext
{
    public MovieContext(DbContextOptions<MovieContext> options) : base(options) { }

    public DbSet<Movie> Movies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<Movie>()
            .ToTable("movies")
            .Property(m => m.Id).HasColumnName("id");
        modelBuilder.Entity<Movie>()
            .Property(m => m.Title).HasColumnName("title");
        modelBuilder.Entity<Movie>()
            .Property(m => m.Genre).HasColumnName("genre");
        modelBuilder.Entity<Movie>()
            .Property(m => m.Description).HasColumnName("description");
        modelBuilder.Entity<Movie>()
            .Property(m => m.Embedding).HasColumnName("embedding")
            .HasColumnType("vector(1536)");
        modelBuilder.Entity<Movie>()
            .Property(m => m.CreatedAt).HasColumnName("created_at");
    }
}

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public string Description { get; set; } = "";
    public Vector? Embedding { get; set; }
    public DateTime CreatedAt { get; set; }
}
