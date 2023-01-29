using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shmear2.BlazorServer.Data;
using Shmear2.Business.Database;
using Shmear2.Business.Configuration;
using Shmear2.Business.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<IConfigurationService>(_ => new ConfigurationService(
    inMemoryDatabaseConnectionString: "InMemoryConnectionString"
));
builder.Services.AddDbContext<CardDbContext>();
// builder.Services.AddScoped<IBoardService, BoardService>();
// builder.Services.AddScoped<ICardService, CardService>();
// builder.Services.AddScoped<IGameService, GameService>();
// builder.Services.AddScoped<IHandService, HandService>();
// builder.Services.AddScoped<IPlayerComputerService, PlayerComputerService>();
// builder.Services.AddScoped<IPlayerService, PlayerService>();
// builder.Services.AddScoped<ITrickService, TrickService>();
builder.Services.AddScoped<IShmearService, ShmearService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
// builder.Services.AddScoped<IPlayerComputerService, PlayerComputerService>();

builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
