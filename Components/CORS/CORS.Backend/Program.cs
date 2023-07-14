using System.Text;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

var restrictedOrigins = "_restrictedOrigins";
var allOrigins = "_allOrigins";
var closedOrigins = "_closedOrigins";

var headlessFrontUrl = builder.Configuration["HeadlessFrontUrl"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: restrictedOrigins,
        policy =>
        {
            policy.WithOrigins(headlessFrontUrl)
                .WithMethods(new string[] { "GET", "PUT" })
                .WithHeaders("x-custom-header")
                .AllowCredentials();
        });
    options.AddPolicy(name: allOrigins,
        policy =>
        {
            policy.WithOrigins("*")
                .WithMethods(new string[] { "GET", "PUT" })
                .WithHeaders("x-custom-header");
        });
    options.AddPolicy(name: closedOrigins,
        policy =>
        {
            policy.WithOrigins("https://26951A4E-B225-4C09-A2F5-49A4E4E6B50B");
        });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/restricted",
        context => context.Response.WriteAsync("restricted"))
    .RequireCors(restrictedOrigins);

app.MapGet("/allorigins",
        context => context.Response.WriteAsync("open"))
    .RequireCors(allOrigins);

app.MapGet("/closed",
        context => context.Response.WriteAsync("closed"))
    .RequireCors(closedOrigins);

app.MapPut("/restricted",
        context => context.Response.WriteAsync("restricted"))
    .RequireCors(restrictedOrigins);

app.MapPut("/allorigins",
        context => context.Response.WriteAsync("open"))
    .RequireCors(allOrigins);

app.MapPut("/closed",
        context => context.Response.WriteAsync("closed"))
    .RequireCors(closedOrigins);

app.Run();