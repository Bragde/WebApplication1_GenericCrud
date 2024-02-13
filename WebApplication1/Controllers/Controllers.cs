using Microsoft.AspNetCore.Mvc;
using SQLitePCL;
using System.Linq.Expressions;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.Services;
namespace WebApplication1.Controllers;

public class StudentController : GenericControllerBase<Student, Student>
{
    private readonly IStudentService _studentService;
    public StudentController(IServiceProvider serviceProvider) : base(serviceProvider) 
    { 
        _studentService = serviceProvider.GetRequiredService<IStudentService>();
    }

    // Test att lägga till en extra metod på en controller
    [HttpGet("getIdOne")]
    public async Task<IActionResult> GetIdOne()
    {
        var id = 1;
        var entity = await _service.Get(id);
        return Ok(entity);
    }

    // Test att overrida en metod från parent controller
    //[HttpGet]
    //public override async Task<IActionResult> List(string[]? includes = null, string? where = null)
    //{
    //    var entities = (await _service.List()).Where(e => e.Id <= 2);
    //    return Ok(entities);
    //}

    // Test att anropa en extra metod utanför GenericCRUDService
    [HttpGet("getIdTwo")]
    public async Task<IActionResult> GetIdTwo()
    {
        var entity = await _studentService.GetIdTwo();
        return Ok(entity);
    }
}

public class CourseController : GenericControllerBase<Course, Course>
{
    public CourseController(IServiceProvider serviceProvider) : base(serviceProvider) { }
}

public class EnrollmentController : GenericControllerBase<Enrollment, Enrollment>
{
    public EnrollmentController(IServiceProvider serviceProvider) : base(serviceProvider) { }
}