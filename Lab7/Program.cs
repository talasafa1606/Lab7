using System.Security.Claims;
using System.Text.Json;
using Azure.Storage.Blobs;
using Lab7;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Lab7.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var keycloakSettings = builder.Configuration.GetSection("Keycloak");

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug); 
});

builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddSingleton(_ => {
    return new BlobServiceClient("UseDevelopmentStorage=true");
});
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakSettings["Authority"];
        
        options.RequireHttpsMetadata = bool.Parse(keycloakSettings["RequireHttpsMetadata"]);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = keycloakSettings["Audience"], 
            ValidIssuer = keycloakSettings["Authority"],
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;

                var realmAccessClaim = claimsIdentity?.FindFirst("realm_access")?.Value;

                if (realmAccessClaim != null)
                {
                    var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim);
                    var roleList = realmAccess.GetProperty("roles").EnumerateArray()
                        .Select(r => r.GetString())
                        .ToList();

                    Console.WriteLine(roleList);

                    foreach (var role in roleList)
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                        Console.WriteLine($"Added role claim: {role}");
                    }
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); 

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Lab7 API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter only the JWT token (without 'Bearer ')."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {} 
        }
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOnly", policy =>
        policy.RequireRole("Student")); 
    options.AddPolicy("TeacherOnly", policy =>
        policy.RequireRole("Teacher"));
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin")); 
});

builder.Services.AddSingleton<RabbitMQPublisher>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<KeycloakUserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();



