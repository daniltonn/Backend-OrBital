namespace Orbital.API.DTOs
{
    public class UsuarioResponseDto
    {
        public int Id_Usuario { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public string Jerarquia { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public DateTime Fecha_Registro { get; set; }

        public int? Nivel_Poder { get; set; }
    }
}