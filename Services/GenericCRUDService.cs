using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WebApplication1.DAL;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.QueryFilter.Extensions;

namespace WebApplication1.Services;

public class GenericCRUDService<TModel, TDto>(IServiceProvider serviceProvider) : IGenericCRUDService<TModel, TDto>
    where TModel : EntityBase
    where TDto : EntityBase
{
    protected readonly IMapper _mapper = serviceProvider.GetRequiredService<IMapper>();
    protected readonly ContosoUniversityContext _context = serviceProvider.GetRequiredService<ContosoUniversityContext>();

    public async Task<IEnumerable<TDto>?> List(string[]? includes = null, string? filter = null)
    {
        // TODO: Gör så svc använder expression för include och filter, så man kan anropa från annat än controllers.
        var query = _context.Set<TModel>()
            .ApplyIncludes(includes)
            .ApplyFilter(filter);
        var entities = (await query.RemoveCyclesAsync()).ToList();
        return _mapper.Map<IEnumerable<TDto>>(entities);
    }

    public async Task<TDto?> Get(int id, string[]? includes = null)
    {
        var query = _context.Set<TModel>()
            .ApplyIncludes(includes)
            .Where(e => e.Id == id);
        var entity = (await query.RemoveCyclesAsync()).FirstOrDefault();
        return _mapper.Map<TDto>(entity);
    }

    public async Task<TDto?> Create(TDto dto)
    {
        var entity = _mapper.Map<TModel>(dto);
        entity!.Created = DateTime.Now;
        await _context.Set<TModel>().AddAsync(entity);
        await _context.SaveChangesAsync();

        return _mapper.Map<TDto>(entity);
    }

    public async Task<TDto?> Update(TDto dto)
    {
        var entity = _mapper.Map<TModel>(dto);
        entity!.Updated = DateTime.Now;
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return _mapper.Map<TDto>(entity);
    }

    public async Task<bool> Delete(int id)
    {
        var entity = await _context.Set<TModel>().FindAsync(id);

        if (entity == null)
            return false;

        _context.Set<TModel>().Remove(entity);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EntityExists(int id)
    {
        return await _context.Set<TModel>().AnyAsync(e => e.Id == id);
    }
}
