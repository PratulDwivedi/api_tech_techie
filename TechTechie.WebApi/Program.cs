using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Net;
using System.Security.Cryptography;
using TechTechie.Services.AI.Interfaces;
using TechTechie.Services.AI.Services;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Common.Services;
using TechTechie.Services.Dynamic.RepositoryInterfaces;
using TechTechie.Services.Dynamic.ServiceInterfaces;
using TechTechie.Services.Dynamic.Services;
using TechTechie.Services.Tenant.ServiceInterfaces;
using TechTechie.Services.Tenant.Services;
using TechTechie.Services.Tenants.RepositoryInterfaces;
using TechTechie.Services.Users.RepositoryInterfaces;
using TechTechie.Services.Users.ServiceInterfaces;
using TechTechie.Services.Users.Services;
using TechTechie.WebApi.Helpers;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------
// 1️⃣ Configuration Setup
// -----------------------------------------------------------
var serveApp = builder.Configuration.GetValue<bool>("ServeApp");
var npgSqlConnection = builder.Configuration.GetValue<string>("ConnectionStrings:NpgSqlconn");
var publicKeyPem = builder.Configuration["Jwt:PublicKey"]
    ?? throw new InvalidOperationException("JWT Public Key configuration is required");

var serviceBrokerOptions = builder.Configuration
    .GetSection("ServiceBroker")
    .Get<ServiceBrokerOptions>();

// -----------------------------------------------------------
// 2️⃣ Core Services
// -----------------------------------------------------------
builder.Services.AddControllers().AddNewtonsoftJson();

// AI HttpClient
builder.Services.AddHttpClient<IAIService, AIService>();

// -----------------------------------------------------------
// 3️⃣ Authentication & Authorization
// -----------------------------------------------------------
var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem);

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = false;
    opt.SaveToken = true;
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new RsaSecurityKey(rsa)
    };
})
.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKeyScheme", options => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ApiKeyScheme");
    });
});

// -----------------------------------------------------------
// 4️⃣ OpenAPI + Scalar Configuration
// -----------------------------------------------------------
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Tech Techie AI API",
            Version = "v1",
            Description = "AI-powered API for Tech Techie services",
        };
        return Task.CompletedTask;
    });
});

// -----------------------------------------------------------
// 5️⃣ Response Compression
// -----------------------------------------------------------
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// -----------------------------------------------------------
// 6️⃣ Service Broker Configuration (Conditional)
// -----------------------------------------------------------
if (serviceBrokerOptions?.MsSqlEnabled == true)
{
    Console.WriteLine("Service Broker is ENABLED");

    builder.Services.AddHttpClient("HttpBrokerClient", client =>
    {
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestVersion = new Version(2, 0);
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new SocketsHttpHandler
        {
            // HTTP/2 and HTTP/3 support
            EnableMultipleHttp2Connections = true,
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),

            // TLS configuration
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                EnabledSslProtocols =
                    System.Security.Authentication.SslProtocols.Tls12 |
                    System.Security.Authentication.SslProtocols.Tls13
            },

            // Compression
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            UseCookies = false
        };

        return handler;
    });

    builder.Services.AddHostedService<MsSqlServiceBrokerService>();
}

// -----------------------------------------------------------
// 7️⃣ Business Services Registration
// -----------------------------------------------------------
builder.Services.AddTransient<ITenantService, TenantService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IDynamicService, DynamicService>();

// -----------------------------------------------------------
// 8️⃣ Repository Registration (Conditional)
// -----------------------------------------------------------
if (!string.IsNullOrEmpty(npgSqlConnection))
{
    builder.Services.AddScoped<TechTechie.PostgresRepository.TenantDbHelper>();
    builder.Services.AddTransient<ITenantRepository, TechTechie.PostgresRepository.Tenants.Repos.TenantRepository>();
    builder.Services.AddTransient<IUserRepository, TechTechie.PostgresRepository.Users.Repos.UserRepository>();
    builder.Services.AddTransient<IDynamicRepository, TechTechie.PostgresRepository.Dynamic.Repos.DynamicRepository>();
}
else
{
    Console.WriteLine("WARNING: PostgreSQL connection string is not configured. Repository services are not registered.");
}

// -----------------------------------------------------------
// 9️⃣ Build Application
// -----------------------------------------------------------
var app = builder.Build();

// -----------------------------------------------------------
// 🔟 Middleware Pipeline
// -----------------------------------------------------------
app.UseResponseCompression();

// JWT token from query string middleware (for GET requests)
app.Use(async (context, next) =>
{
    if (string.IsNullOrWhiteSpace(context.Request.Headers["Authorization"]) &&
        context.Request.QueryString.HasValue)
    {
        var token = context.Request.QueryString.Value
            .Split('&')
            .SingleOrDefault(x => x.Contains("access_token", StringComparison.OrdinalIgnoreCase))
            ?.Split('=')
            .LastOrDefault();

        if (!string.IsNullOrWhiteSpace(token))
        {
            context.Request.Headers.Append("Authorization", $"Bearer {token}");
        }
    }
    await next.Invoke();
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// -----------------------------------------------------------
// 1️⃣1️⃣ SignalR Hub Configuration
// -----------------------------------------------------------
try
{
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapHub<NotificationHub>("notificationHub");
    });
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to configure SignalR notification hub. SignalR functionality may not be available.");
}

// -----------------------------------------------------------
// 1️⃣2️⃣ OpenAPI + Scalar Setup (Non-Production Mode)
// -----------------------------------------------------------
if (!serveApp)
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Tech Techie AI API Documentation")
            .WithTheme(ScalarTheme.Default)
            .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.HttpClient);
    });
}

// -----------------------------------------------------------
// 1️⃣3️⃣ Map Controllers
// -----------------------------------------------------------
app.MapControllers();

// -----------------------------------------------------------
// 1️⃣4️⃣ Static File Serving & Routing
// -----------------------------------------------------------
if (serveApp)
{
    // Serve React App
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}
else
{
    // Redirect to API documentation
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();

    app.MapFallback(() => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();
}

// -----------------------------------------------------------
// 1️⃣5️⃣ Run Application
// -----------------------------------------------------------
app.Run();