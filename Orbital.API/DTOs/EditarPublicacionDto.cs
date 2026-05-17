namespace Orbital.API.DTOs
{
    public class EditarPublicacionDto
    {
        public bool? Activo { get; set; }
        public decimal? Precio_Publicado { get; set; }
        public string? Descripcion_Venta { get; set; }
        public DateTime? Fecha_Vencimiento { get; set; }
    }
}
