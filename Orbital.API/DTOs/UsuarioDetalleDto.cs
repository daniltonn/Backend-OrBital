namespace Orbital.API.DTOs
{
    public class MiembroEquipoDetalleDto
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = null!;
        public string Rol { get; set; } = null!;
        public int Nivel_Poder { get; set; }
    }

    public class EquipoDetalleDto
    {
        public int Id_Equipo { get; set; }
        public string Nombre_Equipo { get; set; } = null!;
        public string Tipo_Equipo { get; set; } = null!;
        public List<MiembroEquipoDetalleDto> Miembros { get; set; } = new();
    }

    public class MisionDetalleDto
    {
        public int Id_Mision { get; set; }
        public string Nombre_Mision { get; set; } = null!;
        public string Tipo_Mision { get; set; } = null!;
        public string Nombre_Planeta { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public byte Prioridad { get; set; }
        public decimal Porcentaje_Avance { get; set; }
        public DateTime Fecha_Asignacion { get; set; }
        public DateTime? Fecha_Inicio { get; set; }
        public DateTime? Fecha_Fin_Estimada { get; set; }
    }

    public class UsuarioDetalleDto
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Contrasena_Hash { get; set; } = null!;
        public string Rol { get; set; } = null!;
        public bool Activo { get; set; }
        public int? Nivel_Poder { get; set; }
        public string Jerarquia { get; set; } = null!;
        public DateTime Fecha_Registro { get; set; }
        public EquipoDetalleDto? Equipo { get; set; }
        public List<MisionDetalleDto> Misiones { get; set; } = new();
    }
}
