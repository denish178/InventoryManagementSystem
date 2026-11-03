
using System.Text;
using InventoryManagement.Api.Data;
using InventoryManagement.Api.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity + Roles
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// JWT settings
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(jwtKey)),

            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });



builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});



var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = services.GetRequiredService<IConfiguration>();

    string[] roles = { "Admin", "Inventory Staff", "Viewer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    var email = configuration["AdminSeed:Email"]
        ?? throw new InvalidOperationException("AdminSeed:Email is missing.");
    var password = configuration["AdminSeed:Password"]
        ?? throw new InvalidOperationException("AdminSeed:Password is missing.");
    var fullName = configuration["AdminSeed:FullName"]
        ?? throw new InvalidOperationException("AdminSeed:FullName is missing.");

    var admin = await userManager.FindByEmailAsync(email);

    if (admin is null)
    {
        admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            IsActive = true,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin creation failed: " +
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(admin, "Admin"))
    {
        var roleResult = await userManager.AddToRoleAsync(admin, "Admin");

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin role assignment failed: " +
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }

    if (configuration.GetValue<bool>("AdminSeed:ResetPassword"))
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(admin);
        var resetResult = await userManager.ResetPasswordAsync(admin, token, password);

        if (!resetResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin password reset failed: " +
                string.Join(", ", resetResult.Errors.Select(e => e.Description)));
        }

        admin.FullName = fullName;
        admin.IsActive = true;

        var updateResult = await userManager.UpdateAsync(admin);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin update failed: " +
                string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
