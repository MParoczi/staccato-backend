using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using ApiModels;
using Application.BackgroundServices;
using Application.Options;
using Application.Services;
using Azure.Storage.Blobs;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Persistence;
using Persistence.Context;
using Persistence.Seed;
using Repository;
using Repository.Repositories;

namespace Application.Extensions;

public static class ServiceCollectionExtensions
{
    private const string CorsPolicyName = "StaccatoPolicy";

    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()!;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };

                // Support JWT from query string for SignalR WebSocket connections.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        CorsConfiguration corsConfiguration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                if (corsConfiguration.AllowedOrigins.Length > 0)
                    policy
                        .WithOrigins(corsConfiguration.AllowedOrigins)
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                // Empty array → no origins configured → all cross-origin requests rejected.
            });
        });

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(context =>
            {
                if (context.Request.Path.StartsWithSegments("/auth"))
                {
                    var remoteIp = context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
                    var rateLimitOptions = context.RequestServices
                        .GetRequiredService<IOptions<RateLimitOptions>>().Value;
                    return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitOptions.AuthMaxRequests,
                            Window = TimeSpan.FromSeconds(rateLimitOptions.AuthWindowSeconds),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                }

                return RateLimitPartition.GetNoLimiter(IPAddress.Loopback);
            });

            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(
                            CultureInfo.InvariantCulture);

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    public static IServiceCollection AddAzureBlob(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var blobOptions = configuration.GetSection("AzureBlob").Get<AzureBlobOptions>()!;
        services.AddSingleton(new BlobServiceClient(blobOptions.ConnectionString));
        services.Configure<AzureBlobOptions>(configuration.GetSection("AzureBlob"));
        return services;
    }

    /// <summary>
    ///     Registers AutoMapper and scans the Application, Api, and Repository assemblies for profiles.
    ///     Repository is included because EntityModel → DomainModel profiles live there.
    /// </summary>
    public static IServiceCollection AddMappingProfiles(this IServiceCollection services)
    {
        services.AddAutoMapper(
            typeof(ServiceCollectionExtensions).Assembly,
            Assembly.Load("Api"),
            Assembly.Load("Repository"));
        return services;
    }

    public static IServiceCollection AddFluentValidationPipeline(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(typeof(ApiModelsAssemblyMarker).Assembly);
        return services;
    }

    public static IServiceCollection AddSignalRHub(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<PdfExportBackgroundService>();
        services.AddHostedService<ExportCleanupService>();
        services.AddHostedService<AccountDeletionCleanupService>();
        return services;
    }

    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<InstrumentSeeder>();
        services.AddScoped<ChordSeeder>();
        services.AddScoped<SystemStylePresetSeeder>();
        services.AddScoped<DbInitializer>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserSavedPresetRepository, UserSavedPresetRepository>();
        services.AddScoped<IInstrumentRepository, InstrumentRepository>();
        services.AddScoped<IChordRepository, ChordRepository>();
        services.AddScoped<INotebookRepository, NotebookRepository>();
        services.AddScoped<INotebookModuleStyleRepository, NotebookModuleStyleRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ILessonPageRepository, LessonPageRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<IPdfExportRepository, PdfExportRepository>();
        services.AddScoped<ISystemStylePresetRepository, SystemStylePresetRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    public static IServiceCollection AddLocalizationSupport(this IServiceCollection services)
    {
        services.AddLocalization();

        services.AddRequestLocalization(options =>
        {
            var supportedCultures = new[] { "en", "hu" };
            options.SetDefaultCulture("en")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            // Only use Accept-Language header; strip region suffix (e.g. en-US → en).
            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(
                new AcceptLanguageHeaderRequestCultureProvider());
        });

        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<IAzureBlobService, AzureBlobService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IChordService, ChordService>();
        services.AddScoped<IInstrumentService, InstrumentService>();
        services.AddScoped<INotebookService, NotebookService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ILessonPageService, LessonPageService>();
        services.AddResponseCaching();
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Staccato API",
                Version = "v1",
                Description = "Backend API for the Staccato instrument learning notebook application."
            });

            var bearerScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT access token."
            };
            options.AddSecurityDefinition("Bearer", bearerScheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}