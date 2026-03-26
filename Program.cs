using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;
using MaalCaCMS.Composers;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add CORS policy for maalca-web frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("MaalcaWeb", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://maalca.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

// Apply CORS policy
app.UseCors("MaalcaWeb");

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
