namespace Orbital.API.DTOs
{
    public class ClienteRegistroDto
    {
        public string Nombre { get; set; } = null!;
        public string Tipo_Cliente { get; set; } = "Individuo";
        public int? Id_Galaxia_Origen { get; set; }
        public string Correo { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class ClienteLoginDto
    {
        public string Correo { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class ClienteLoginResponseDto
    {
        public string Token { get; set; } = null!;
        public int Id_Cliente { get; set; }
        public string Nombre { get; set; } = null!;
    }

    public class ClienteResponseDto
    {
        public int Id_Cliente { get; set; }
        public string Nombre { get; set; } = null!;
        public string Tipo_Cliente { get; set; } = null!;
        public string? Galaxia_Origen { get; set; }
        public string Correo { get; set; } = null!;
        public decimal Credito_Disponible { get; set; }
        public string Nivel_Confianza { get; set; } = null!;
        public DateTime Fecha_Registro { get; set; }
        public bool Activo { get; set; }
    }

    public class ClienteUpdateDto
    {
        public string? Nombre { get; set; }
        public string? Tipo_Cliente { get; set; }
        public int? Id_Galaxia_Origen { get; set; }
        public string? Correo { get; set; }
        public string? Nivel_Confianza { get; set; }
    }

    public class CreditoAjusteDto
    {
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = null!;
    }

    public class ClienteCreateAdminDto
    {
        public string Nombre { get; set; } = null!;
        public string Tipo_Cliente { get; set; } = "Individuo";
        public int? Id_Galaxia_Origen { get; set; }
        public string Correo { get; set; } = null!;
        public string Password { get; set; } = null!;
        public decimal Credito_Disponible { get; set; } = 0;
        public string Nivel_Confianza { get; set; } = "Nuevo";
    }

    public class ClienteDetalleDto : ClienteResponseDto
    {
        public List<TransaccionClienteDto> Compras { get; set; } = new();
    }
}
