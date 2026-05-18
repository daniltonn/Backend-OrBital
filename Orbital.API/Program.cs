using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Orbital.API.Data;
using Orbital.API.Repositories;
using Orbital.API.Services;
using System.Text;
using Orbital.API.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Conexion con DB (MySQL - Pomelo)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("10.4.32-mariadb")));

// set base cofiguracion JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

// Conexion con frontend (CORS)
var origenesPermitidos = builder.Configuration
    .GetValue<string>("OrigenesPermitidos")?
    .Split(",") ?? new string[] { "*" };

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],

        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// Inyección de dependencias - Repositorios
builder.Services.AddScoped<IPlanetasRepository, PlanetasRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IPlanetaEstadoRepository, PlanetaEstadoRepository>();
builder.Services.AddScoped<IRecursoPlanetarioRepository, RecursoPlanetarioRepository>();
builder.Services.AddScoped<IRecursoRepository, RecursoRepository>();
// Inyección de dependencias - Servicios
builder.Services.AddScoped<IPlanetasService, PlanetasService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<PlanetaEstadoService>();
builder.Services.AddScoped<IValoracionService, ValoracionService>();
builder.Services.AddScoped<IRecursoService, RecursoService>();
builder.Services.AddScoped<IRecursoPlanetarioService, RecursoPlanetarioService>();
builder.Services.AddScoped<GalaxiaService>();
builder.Services.AddScoped<AtmosferaService>();
// Mercado Interestelar
builder.Services.AddScoped<IMercadoService, MercadoService>();
builder.Services.AddScoped<ITransaccionService, TransaccionService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IClienteAuthService, ClienteAuthService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
// Servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Ingresla el Token JWT Aqui Autenticar las peticiones",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddCustomAuthorization();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Cors
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


