
using GjettLataBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RoomManager>();
builder.Services.AddSingleton<SpotifyService>();
builder.Services.AddHttpClient<DeezerService>();
builder.Services.AddSignalR();

builder.Services.AddControllers();

var allowedOrigin = builder.Configuration["CORS_ORIGIN"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigin!)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RoomHub>("/hub/room");

app.Run();