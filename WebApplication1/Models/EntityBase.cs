namespace WebApplication1.Models;

public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
}