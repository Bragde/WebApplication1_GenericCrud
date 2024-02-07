using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface IStudentService
{
    Task<Student?> GetIdTwo();
}
