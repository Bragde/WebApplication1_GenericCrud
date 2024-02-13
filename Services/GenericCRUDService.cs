using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WebApplication1.DAL;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.QueryFilter;

namespace WebApplication1.Services;

public class GenericCRUDService<TModel, TDto>(IServiceProvider serviceProvider) : IGenericCRUDService<TModel, TDto>
    where TModel : EntityBase
    where TDto : EntityBase
{
    protected readonly IMapper _mapper = serviceProvider.GetRequiredService<IMapper>();
    protected readonly ContosoUniversityContext _context = serviceProvider.GetRequiredService<ContosoUniversityContext>();

    public async Task<IEnumerable<TDto>?> List(string[]? includes = null, string? where = null)
    {
        var query = _context.Set<TModel>().ApplyIncludes(includes);
        var entities = (await query.RemoveCyclesAsync()).ToList();

        //if (where != null)
        //    query = query.Where(where);

        return _mapper.Map<IEnumerable<TDto>>(entities);
    }

    public async Task<TDto?> Get(int id)
    {
        var entity = await _context.Set<TModel>().FindAsync(id);
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

    private IQueryable<TModel> ApplyIncludes(IQueryable<TModel> query, params string[] includes)
    {
        return includes == null 
            ? query 
            : includes.Aggregate(query, (current, include) => current.Include(include));
    }
}
