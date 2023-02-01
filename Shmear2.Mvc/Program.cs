using Shmear2.Business.Database;
using Shmear2.Business.Configuration;
using Shmear2.Business.Services;
using Shmear2.Mvc.Hubs;

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
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
