using Shmear2.Business.Services;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;
using Shmear2.Business.Database;
using Shmear2.Business.Configuration;
using Shmear2.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CardDbContext>();
builder.Services.AddScoped<IShmearService, ShmearService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
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

app.MapGet("/", () => "Hello World!");

app.UseCors("AllowedCorsOrigins");

app.MapHub<ShmearHub>("/shmearhub");

app.Run();
