using GainIt.API.Data;
using GainIt.API.Services.Projects.Implementations;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IProjectService, ProjectService>();

builder.Services.AddDbContext<GainItDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GainItPostgresDb")));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
