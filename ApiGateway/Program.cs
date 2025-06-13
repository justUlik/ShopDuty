var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("http://localhost:5050/swagger/v1/swagger.json", "Orders API");
    c.SwaggerEndpoint("http://localhost:5001/swagger/v1/swagger.json", "Payments API");
    c.RoutePrefix = "swagger"; 
});

app.MapReverseProxy();

app.Run();