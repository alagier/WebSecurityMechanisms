var builder = WebApplication.CreateBuilder(args);

const string restrictedOrigins = "_restrictedOrigins";
const string allOrigins = "_allOrigins";
const string closedOrigins = "_closedOrigins";

var headlessFrontUrl =
    builder.Configuration["HeadlessFrontUrl"] ?? throw new Exception("headlessFrontUrl can't be null");

var httpMethods = new[]
{
    "HEAD", "GET", "PUT", "POST", "DELETE", "OPTIONS", "PATCH"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: restrictedOrigins,
        policy =>
        {
            policy.WithOrigins(headlessFrontUrl)
                .WithMethods(httpMethods)
                .WithHeaders("x-custom-header")
                .AllowCredentials();
        });
    options.AddPolicy(name: allOrigins,
        policy =>
        {
            policy.WithOrigins("*")
                .WithMethods(httpMethods)
                .WithHeaders("x-custom-header");
        });
    options.AddPolicy(name: closedOrigins,
        policy => { policy.WithOrigins("https://26951A4E-B225-4C09-A2F5-49A4E4E6B50B"); });
});

var app = builder.Build();

app.UseCors();

app.MapMethods("/restricted", httpMethods,
        context => context.Response.WriteAsync("restricted"))
    .RequireCors(restrictedOrigins);

app.MapMethods("/allorigins", httpMethods,
        context => context.Response.WriteAsync("open"))
    .RequireCors(allOrigins);

app.MapMethods("/closed", httpMethods,
context => context.Response.WriteAsync("closed"))
    .RequireCors(closedOrigins);

app.Run();