using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using WebApplication1;
using WebApplication1.DAL;
using WebApplication1.Data;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database context
builder.Services.AddDbContext<ContosoUniversityContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ContosoUniversityContext_SQLlocaldb"));
});

// Add services
builder.Services
    .AddScoped<IGenericCRUDService<Student, Student>, GenericCRUDService<Student, Student>>()
    .AddScoped<IGenericCRUDService<Course, Course>, GenericCRUDService<Course, Course>>()
    .AddScoped<IGenericCRUDService<Enrollment, Enrollment>, GenericCRUDService<Enrollment, Enrollment>>()
    .AddScoped<IStudentService, StudentService>();

// Add mapper configuration
builder.Services.AddMapperConfiguration();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage(); // <- https://learn.microsoft.com/en-us/aspnet/core/data/ef-rp/intro?view=aspnetcore-8.0&tabs=visual-studio
}

// Create database if it doesnt exist
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ContosoUniversityContext>();
    context.Database.EnsureCreated(); // Use before start using migrations
    //await context.Database.MigrateAsync();
    DbInitializer.Initialize(context); // Use to seed testdata
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();