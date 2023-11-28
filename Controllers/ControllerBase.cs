using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenericControllerBase<T>: ControllerBase where T: EntityBase
{
    protected readonly DataContext _context;

    public GenericControllerBase(DataContext dataContext)
    {
        _context = dataContext;
    }
}

