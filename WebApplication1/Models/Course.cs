using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class Course: EntityBase
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public new int Id { get; set; } // Hide base member Id
    public string Title { get; set; } = string.Empty;
    public int Credits { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; }
}

