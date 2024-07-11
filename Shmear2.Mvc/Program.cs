using Shmear2.Business.Database;
using Shmear2.Business.Configuration;
using Shmear2.Business.Services;
using Shmear2.Mvc.Hubs;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

Console.WriteLine($"EnvironmentName: {builder.Environment.EnvironmentName}");

// builder.Configuration
//         .SetBasePath(Directory.GetCurrentDirectory())
//         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//         .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
//         .AddEnvironmentVariables();

if (builder.Environment.EnvironmentName == "Development")
{
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();
}
else
{
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();
}

logger.LogInformation("Kestrel configuration: {KestrelConfig}", 
    builder.Configuration.GetSection("Kestrel").Get<Dictionary<string, object>>());

var endpoints = builder.Configuration.GetSection("Kestrel:Endpoints")
    .Get<Dictionary<string, Dictionary<string, object>>>();

if (endpoints != null)
{
    foreach (var endpoint in endpoints)
    {
        logger.LogInformation("Endpoint {EndpointName}: {EndpointConfig}", 
            endpoint.Key, endpoint.Value);
    }
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CardDbContext>();
builder.Services.AddScoped<IShmearService, ShmearService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IPlayerComputerService, PlayerComputerService>();
builder.Services.AddSingleton<IConfigurationService>(_ =>
    new ConfigurationService(inMemoryDatabaseConnectionString: "InMemoryDatabaseConnectionString"));
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: "AllowedCorsOrigins",
                    builder =>
                    {
                        builder.AllowAnyHeader()
                            .AllowAnyMethod()
                            .SetIsOriginAllowed((host) => true)
                            .AllowCredentials();
                    });
            });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowedCorsOrigins");
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ShmearHub>("/shmearhub");
//endpoints.MapHub<ShmearHub>("/shmearhub");

app.Run();
