using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DAL;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq.Expressions;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class GenericControllerBase<TModel, TDto>(IServiceProvider serviceProvider) : ControllerBase 
    where TModel: EntityBase
    where TDto : EntityBase
{
    protected readonly IGenericCRUDService<TModel, TDto> _service = serviceProvider.GetRequiredService<IGenericCRUDService<TModel, TDto>>();

    [HttpGet]
    public virtual async Task<IActionResult> List([FromQuery] string[]? includes = null, string? where = null)
    {
        var entities = await _service.List(includes, where);
        return Ok(entities);
    }

    [HttpGet("{id}")]
    public virtual async Task<IActionResult> Get(int id, [FromQuery] string[]? includes = null)
    {
        var entity = await _service.Get(id, includes);
        if (entity == null) 
            return NotFound();

        return Ok(entity);
    }

    [HttpPost]
    public virtual async Task<IActionResult> Create(TDto dto)
    {              
        var entity = await _service.Create(dto);
        return CreatedAtAction("Get", new { id = entity.Id }, entity);
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(int id, TDto dto)
    {
        if (id !=  dto.Id)
            return BadRequest();

        if (!await _service.EntityExists(dto.Id))
            return NotFound();

        await _service.Update(dto);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.Delete(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}

