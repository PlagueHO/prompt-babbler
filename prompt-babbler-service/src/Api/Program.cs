using PromptBabbler.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddInfrastructure();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddProblemDetails();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
