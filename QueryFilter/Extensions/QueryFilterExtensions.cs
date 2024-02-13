using System.Reflection;

namespace WebApplication1.QueryFilter.Extensions;

public static class QueryFilterExtensions
{
    /// <summary>
    /// Applies the filter to the given query
    /// </summary>
    public static IQueryable<TSource> ApplyFilter<TSource>(this IQueryable<TSource> source, string? filter) where TSource : class
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return source;
        }

        try
        {
            var _filter = QueryFilterParser.Parse(filter, source);

            return _filter == null ? source : source.Where(_filter);
        }
        catch (TargetInvocationException ex)
        {
            // We do a bit of exception unwrapping here, trying to find the most relevant exception
            var inner = ex.InnerException;
            while (inner != null)
            {
                if (inner is not TargetInvocationException)
                {
                    throw inner;
                }
                inner = inner.InnerException;
            }
            throw;
        }
    }
}
