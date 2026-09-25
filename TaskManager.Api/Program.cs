
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using TaskManager.Api.Filters;
using TaskManager.Api.Middlewares;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Options;
using TaskManager.Application.Validators;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Services;
namespace TaskManager.Api
{
    public class Program
    {
        private static readonly JwtSecurityTokenHandler tokenHandler = new();
        public static void Main(string[] args)
        {

            // Logger Configuration
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information() 
                .WriteTo.Console() 
                .WriteTo.File(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter() ,"Logs/log-.txt", rollingInterval: RollingInterval.Day) // Save logs in daily file in logs folder 
                .CreateLogger();

            try
            {
                Log.Information("Starting Web Host...");

                var builder = WebApplication.CreateBuilder(args);

                // 1. Bind Options Pattern
                builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

                // 2. Database Context Configuration (MySQL)
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

                // 3.0 Register Application Services (Dependency Injection)
                builder.Services.AddScoped<IAuthService, AuthService>();
                builder.Services.AddScoped<ICategoryService, CategoryService>();
                builder.Services.AddScoped<ISessionService, SessionService>();

                // 3.1 Register Middlewares
                builder.Services.AddTransient<ExceptionHandlingMiddleware>();

                // 3.2 Register ALL FluentValidation
                builder.Services.AddValidatorsFromAssemblyContaining<CreateSessionDtoValidator>();


                // 4. JWT Authentication Setup
                var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

                if(string.IsNullOrWhiteSpace(jwtOptions?.SigningKey) || Encoding.UTF8.GetBytes(jwtOptions.SigningKey).Length < 32 )
                {
                    throw new InvalidOperationException("JWT SigningKey is missing or too short! It must be at least 256 bits (32 bytes).");
                }
                var key = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);

                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions?.Issuer,
                        ValidAudience = jwtOptions?.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                });

                // Add services to the container.

                builder.Services.AddControllers(options =>
                {
                    options.Filters.Add<ValidationFilter>();

                })
                    // Convert any enum to string by default
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

                // Configure Rate Limiting Policies
                builder.Services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    options.AddPolicy("AuthPolicy", httpContext =>
                    {
                        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: ipAddress,
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0
                            }
                        );
                    });

                    options.AddPolicy("StandardPolicy", httpContext =>
                    {
                        string partitionKey = GetPartitionKey(httpContext);

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: partitionKey,
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 10,
                                Window = TimeSpan.FromSeconds(10),
                                QueueLimit = 0
                            }
                        );
                    });
                });


                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // Use serilog as default logging providers an in DI
                builder.Host.UseSerilog();

                var app = builder.Build();

                // Add middlewere 
                app.UseSerilogRequestLogging();


                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();

                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.UseRouting();
                app.UseRateLimiter();


                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }
        private static string GetPartitionKey(HttpContext httpContext)
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                try
                {
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                    if (!String.IsNullOrEmpty(userId))
                        return $"user_{userId}";
                }
                catch {}
            }

            string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            return $"ip_{ip}";
        }
    }
}
