using System.Linq.Expressions;
using WebApplication1.Models;

namespace WebApplication1.DTOs
{
    public class GetRequest<T> where T : EntityBase
    {
        public Expression<Func<T, bool>>? Where { get; set; } = null;
        public int MyProperty { get; set; }
    }
}
