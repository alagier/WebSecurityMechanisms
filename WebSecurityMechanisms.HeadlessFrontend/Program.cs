var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", context =>
{
    context.Response.Cookies.Append("MyCookie", "XXX");

    return context.Response.WriteAsync("");
});

app.Run();