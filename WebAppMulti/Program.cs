using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Text;
using WebAppMulti.Database.Auth;
using WebAppMulti.Database.Repository;
using WebAppMulti.Middleware;
using WebAppMulti.Services;
using WebAppMulti.Services.CaseManagement;
//using WebAppMulti.Modules.Cases;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "ApplicationLogs",
            AutoCreateSqlTable = true
        },
        restrictedToMinimumLevel: LogEventLevel.Error)
    .CreateLogger();

builder.Host.UseSerilog();

var pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE");
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

builder.Services.AddScoped<GenericRepository>();
builder.Services.AddScoped<DapperRepository>();
builder.Services.AddScoped<SessionDocumentService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<FormTemplateService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<EFdbServie>();
builder.Services.AddScoped<IUserStore, DummyAuthStore>();
builder.Services.AddScoped<MyMarker>();

builder.Services.AddHttpClient();


builder.Services.AddDbContext<WebAppMulti.Database.MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WebAppMulti API", Version = "v1" });
    c.SwaggerDoc("CaseManagement", new()
    {
        Title = "Case Management",
        Version = "v1",
        Description = "Case Management API<br/><a href='/table' target='_blank'>Open Case Management UI</a>"
    });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (docName == "CaseManagement")
            return apiDesc.GroupName == "Case Management";

        return true;
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token."
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
            Array.Empty<string>()
        }
    });
});

builder.WebHost.UseUrls("http://0.0.0.0:80");

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseSerilogRequestLogging();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<BeforeAfterMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();

app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        var pb = httpReq.HttpContext.Request.PathBase.Value;
        if (!string.IsNullOrEmpty(pb))
        {
            swaggerDoc.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
            {
                new() { Url = pb }
            };
        }
    });
});

app.UseSwaggerUI(c =>
{
    var basePath = string.IsNullOrEmpty(pathBase) ? "" : pathBase;
    c.SwaggerEndpoint($"{basePath}/swagger/v1/swagger.json", "WebAppMulti API v1");
    c.SwaggerEndpoint($"{basePath}/swagger/CaseManagement/swagger.json", "Case Management API");
    c.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapCasesEndpoints();

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

app.MapFallbackToFile("index.html");

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
