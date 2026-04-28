using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
//using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebAppMulti.Database.Auth;
using WebAppMulti.Database.Repository;
using WebAppMulti.Middleware;
using WebAppMulti.Services;
using WebAppMulti.Services.CaseManagement;
using Serilog;
using Serilog.Events;


var builder = WebApplication.CreateBuilder(args);


// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)

    .WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
        {
            TableName = "ApplicationLogs",
            AutoCreateSqlTable = true
        },
        restrictedToMinimumLevel: LogEventLevel.Error)
    .CreateLogger();

builder.Host.UseSerilog();


// Read path base from environment variable (e.g., "/app1")
var pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE");

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

builder.Services.AddScoped<GenericRepository>();
builder.Services.AddScoped<DapperRepository>();


// Register CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Services
builder.Services.AddScoped<SessionDocumentService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<FormTemplateService>();


builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<EFdbServie>();
builder.Services.AddDbContext<WebAppMulti.Database.MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserStore, DummyAuthStore>();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<MyMarker>();
builder.Services.AddHttpClient();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WebAppMulti API", Version = "v1" });
    c.SwaggerDoc("CaseManagement", new() { Title = "Case Management", Version = "v1", Description = "Case Management API<br/><a href='/table' target='_blank'>Open Case Management UI</a>" });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (docName == "CaseManagement")
            return apiDesc.GroupName == "Case Management";
        return true; // default v1
    });

    // JWT support in Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.WebHost.UseUrls("http://0.0.0.0:80");

var app = builder.Build();

// Apply forwarded headers (important when behind Nginx reverse proxy)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Apply CORS
app.UseCors("AllowAll");

//Begin 12/21/25
app.UseDefaultFiles();
app.UseStaticFiles();


app.MapControllers();

// React routes fallback
app.MapFallbackToFile("index.html");
// END 1221/25




// Apply PathBase if defined
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

// Custom middleware
app.UseMiddleware<BeforeAfterMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();

// Swagger configuration
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            var pb = httpReq.HttpContext.Request.PathBase.Value;
            if (!string.IsNullOrEmpty(pb))
            {
                swaggerDoc.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
                {
                    new Microsoft.OpenApi.Models.OpenApiServer { Url = pb }
                };
            }
        });
    });

    app.UseSwaggerUI(c =>
    {
        var swaggerJsonBasePath = string.IsNullOrEmpty(pathBase) ? "" : pathBase;
        c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/v1/swagger.json", "My API V1");
        c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/CaseManagement/swagger.json", "Case Management");
        c.RoutePrefix = "swagger";
    });
}

// Routing
app.UseRouting();

// Test endpoint
app.MapGet("/", (IUserStore userStore) =>
{
    var user = userStore.FindByUsername("frank");
    if (user == null) return "User not found";

    var principal = userStore.CreatePrincipal(user);
    return $"Hello {user.UserName}, Roles: {string.Join(", ", user.Roles)}";
});

app.MapGet("/test-error", () =>
{
    throw new Exception("This is a test exception for Serilog.");
});


//app.MapGet("/api/table", () =>
//{
//    return Results.Redirect("/table");
//})
//.WithName("HomeTable")
//.WithTags("Home")
//.WithSummary("Home")
//.WithDescription("Redirects to the Table UI page");
////

////
// Auth
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

//app.Run();
try
{
    Log.Information("Application starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
