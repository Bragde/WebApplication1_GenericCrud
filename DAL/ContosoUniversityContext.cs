using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.DAL;

public class ContosoUniversityContext : DbContext
{
    public ContosoUniversityContext(DbContextOptions<ContosoUniversityContext> options) : base(options) { }

    public DbSet<Student> Students { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Course> Courses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}

