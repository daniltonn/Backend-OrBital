namespace Orbital.API.DTOs
{
    public class PlanetaResumenMercadoDto
    {
        public int Id_Planeta { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Color1 { get; set; }
        public string? Color2 { get; set; }
        public string? Color3 { get; set; }
        public string Estado { get; set; } = null!;
        public string? TipoAtmosfera { get; set; }
        public string NivelTecnologico { get; set; } = null!;
    }

    public class MercadoListItemDto
    {
        public int Id_Publicacion { get; set; }
        public string Clase_Planeta { get; set; } = null!;
        public decimal Valor_Total { get; set; }
        public decimal Precio_Publicado { get; set; }
        public decimal Precio_Minimo { get; set; }
        public DateTime? Fecha_Vencimiento { get; set; }
        public DateTime Fecha_Publicacion { get; set; }
        public string? Descripcion_Venta { get; set; }
        public bool Activo { get; set; }
        public PlanetaResumenMercadoDto Planeta { get; set; } = null!;
    }
}
