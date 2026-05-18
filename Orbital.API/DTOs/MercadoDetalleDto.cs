namespace Orbital.API.DTOs
{
    public class MercadoDetalleDto
    {
        public int Id_Publicacion { get; set; }
        public string Clase_Planeta { get; set; } = null!;
        public decimal Precio_Publicado { get; set; }
        public decimal Precio_Minimo { get; set; }
        public DateTime? Fecha_Vencimiento { get; set; }
        public string? Descripcion_Venta { get; set; }
        public PlanetaDetalleMercadoDto Planeta { get; set; } = null!;
        public ValoracionMercadoDto Valoracion { get; set; } = null!;
    }

    public class PlanetaDetalleMercadoDto
    {
        public int Id_Planeta { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string? Color1 { get; set; }
        public string? Color2 { get; set; }
        public string? Color3 { get; set; }
        public string Estado { get; set; } = null!;
        public string? TipoAtmosfera { get; set; }
        public string? DescripcionAtmosfera { get; set; }
        public string NivelTecnologico { get; set; } = null!;
        public List<RecursoPlanetaMercadoDto> Recursos { get; set; } = new();
        public CoordenadasMercadoDto? Coordenadas { get; set; }
    }

    public class ValoracionMercadoDto
    {
        public decimal Recursos_Score { get; set; }
        public decimal Tecnologia_Score { get; set; }
        public decimal Ubicacion_Score { get; set; }
        public decimal Poder_Score { get; set; }
        public decimal Riesgo_Score { get; set; }
        public decimal Valor_Total { get; set; }
        public decimal Precio_Final { get; set; }
    }

    public class RecursoPlanetaMercadoDto
    {
        public string Nombre_Recurso { get; set; } = null!;
        public decimal Cantidad_Estimada { get; set; }
        public string Rareza { get; set; } = null!;
    }

    public class CoordenadasMercadoDto
    {
        public decimal Coordenada_X { get; set; }
        public decimal Coordenada_Y { get; set; }
        public decimal Coordenada_Z { get; set; }
    }
}
