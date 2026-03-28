using Application.Extensions;
using Application.Hubs;
using Application.Middleware;
using Application.Options;
using Persistence;
using QuestPDF;
using QuestPDF.Infrastructure;

Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Options — bound, validated on startup; any misconfiguration causes immediate failure.
builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<AzureBlobOptions>()
    .BindConfiguration("AzureBlob")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<CorsConfiguration>()
    .BindConfiguration("Cors")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<RateLimitOptions>()
    .BindConfiguration("RateLimit")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<GoogleOptions>()
    .BindConfiguration("Google")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Read values needed as parameters before Build() — do NOT use BuildServiceProvider().
var corsConfiguration = builder.Configuration.GetSection("Cors").Get<CorsConfiguration>()!;

// Service registrations.
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddCorsPolicy(corsConfiguration);
builder.Services.AddRateLimiting();
builder.Services.AddAzureBlob(builder.Configuration);
builder.Services.AddMappingProfiles();
builder.Services.AddFluentValidationPipeline();
builder.Services.AddSignalRHub();
builder.Services.AddBackgroundWorkers();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddLocalizationSupport();
builder.Services.AddDomainServices();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwagger();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

// Middleware pipeline — exact order per FR-024.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRequestLocalization();
app.UseExceptionHandler();
app.UseMiddleware<BusinessExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("StaccatoPolicy"); // must match ServiceCollectionExtensions.CorsPolicyName
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapControllers();

app.Run();

// Expose Program to the Tests project for WebApplicationFactory<Program>.
public partial class Program
{
}