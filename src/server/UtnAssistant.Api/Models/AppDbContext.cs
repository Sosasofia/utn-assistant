using Microsoft.EntityFrameworkCore;

namespace UtnAssistant.API.Models;

public class AppDbContext : DbContext
{
    public DbSet<Career> Careers => Set<Career>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Correlative> Correlatives => Set<Correlative>();
    public DbSet<SyllabusChunk> SyllabusChunks => Set<SyllabusChunk>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSubjectProgress> UserSubjectProgresses => Set<UserSubjectProgress>();

    public DbSet<CareerChunk> CareerChunks => Set<CareerChunk>();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Career>()
            .HasIndex(c => c.Code)
            .IsUnique();

        modelBuilder.Entity<Subject>()
            .HasIndex(s => new { s.CareerId, s.Code })
            .IsUnique();

        modelBuilder.Entity<Subject>()
            .HasIndex(s => new { s.Year, s.Semester });

        modelBuilder.Entity<Correlative>()
            .HasIndex(c => new { c.SubjectId, c.RequiredSubjectId, c.Type })
            .IsUnique();

        modelBuilder.Entity<Correlative>()
            .HasOne(c => c.Subject)
            .WithMany(s => s.CorrelativesAsTarget)
            .HasForeignKey(c => c.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Correlative>()
            .HasOne(c => c.RequiredSubject)
            .WithMany(s => s.CorrelativesAsPrereq)
            .HasForeignKey(c => c.RequiredSubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<UserSubjectProgress>()
            .HasIndex(p => new { p.UserId, p.SubjectId })
            .IsUnique();

        modelBuilder.Entity<SyllabusChunk>()
             .Property(s => s.Embedding)
             .HasColumnType("vector(1536)");

        modelBuilder.Entity<CareerChunk>()
             .Property(s => s.Embedding)
             .HasColumnType("vector(1536)");

        base.OnModelCreating(modelBuilder);
    }
}