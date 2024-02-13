using AutoMapper;
using WebApplication1.Models;

namespace WebApplication1;

internal static class MapperConfig
{
    public static IServiceCollection AddMapperConfiguration(this IServiceCollection services) => services.AddSingleton(_config.CreateMapper());

    private static readonly MapperConfiguration _config = new(cfg =>
    {
        cfg.CreateMap<Student, Student>();
        cfg.CreateMap<Course, Course>();
        cfg.CreateMap<Enrollment, Enrollment>();
    });
}
