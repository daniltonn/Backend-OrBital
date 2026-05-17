namespace Orbital.API.DTOs
{
    public class MercadoListItemDto
    {
        public int Id_Publicacion { get; set; }
        public int Id_Planeta { get; set; }
        public string Nombre_Planeta { get; set; } = null!;
        public string Galaxia { get; set; } = null!;
        public string? Color1 { get; set; }
        public string? Color2 { get; set; }
        public string? Color3 { get; set; }
        public string Estado_Planeta { get; set; } = null!;
        public decimal Precio_Publicado { get; set; }
        public string Clase_Planeta { get; set; } = null!;
        public decimal Valor_Total { get; set; }
        public decimal Precio_Final_Valoracion { get; set; }
        public string? Descripcion_Venta { get; set; }
        public DateTime? Fecha_Vencimiento { get; set; }
        public DateTime Fecha_Publicacion { get; set; }
    }
}
