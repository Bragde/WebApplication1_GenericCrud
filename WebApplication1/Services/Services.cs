using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class StudentService : GenericCRUDService<Student, Student>, IStudentService
{
    public StudentService(IServiceProvider serviceProvider) : base(serviceProvider) { }

    // Test att lägga till en extra metod i en service
    public async Task<Student?> GetIdTwo()
    {
        var id = 2;
        var entity = await _context.Set<Student>().FindAsync(id);
        return _mapper.Map<Student>(entity);
    }
}

//public class CourseService : GenericCRUDService<Course, Course>
//{
//    public CourseService(IServiceProvider serviceProvider) : base(serviceProvider) { }
//}

//public class EnrollmentService : GenericCRUDService<Enrollment, Enrollment>
//{
//    public EnrollmentService(IServiceProvider serviceProvider) : base(serviceProvider) { }
//}