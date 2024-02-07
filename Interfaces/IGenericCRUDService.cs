using System.Linq.Expressions;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface IGenericCRUDService<TModel, TDto>
    where TModel : EntityBase
    where TDto : EntityBase
{
    Task<IEnumerable<TDto>?> List(string[]? includes = null, string? where = null);
    Task<TDto?> Get(int id);
    Task<TDto?> Create(TDto dto);
    Task<TDto?> Update(TDto dto);
    Task<bool> Delete(int id);
    Task<bool> EntityExists(int id);
}