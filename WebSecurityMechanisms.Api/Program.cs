
using WebSecurityMechanisms.Api.Providers;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using WebSecurityMechanisms.Api.Repositories;
using WebSecurityMechanisms.Api.Repositories.Interfaces;
using WebSecurityMechanisms.Api.Services;
using WebSecurityMechanisms.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ICorsService, CorsService>();
builder.Services.AddScoped<IHeadlessBrowserProvider, HeadlessBrowserProvider>();
builder.Services.AddScoped<IDiagramProvider, DiagramProvider>();
builder.Services.AddScoped<ICorsRepository, CorsRepository>();
builder.Services.AddScoped<IProxyRepository, ProxyRepository>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:5228", "http://localhost").AllowAnyHeader();
        });
});

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
    app.UseExceptionHandler("/error-development");
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();