using Orbital.API.DTOs;
using Orbital.API.Models;
using Orbital.API.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.Text;


namespace Orbital.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _repo;
        private readonly IConfiguration _configuration;

        public AuthService(IUsuarioRepository repo, IConfiguration configuration)
        {
            _repo = repo;
            _configuration = configuration;
        }

        public async Task<ResponseLoginDto?> Login(UsuarioLoginDto dto)
        {
            var usuario = await _repo.ObtenerPorEmail(dto.Correo.ToLower());

            if (usuario == null)
                return null;

            bool passwordValida;
            try
            {
                passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.Contrasena_Hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return null;
            }

            if (!passwordValida)
                return null;

            var Key = GenerateJwtToken(usuario);

            return new ResponseLoginDto
            {
                Token = Key
            };
        }

        public async Task<Usuario> Register(UsuarioCreateDto dto)
        {
            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Correo = dto.Correo.ToLower(),
                Contrasena_Hash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Id_Rol = dto.Id_Rol,
                Id_Jerarquia = dto.Id_Jerarquia,
                Activo = true,
                Fecha_Registro = DateTime.Now
            };

            await _repo.Crear(usuario);

            return usuario;
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, usuario.Id_Usuario.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
        new Claim("Id_Rol", usuario.Id_Rol.ToString())
    };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}