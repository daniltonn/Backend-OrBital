namespace Orbital.API.DTOs
{
    public class PlanetaResumenValoracionDto
    {
        public string Nombre { get; set; } = null!;
        public string? Color1 { get; set; }
        public string? Color2 { get; set; }
        public string? Color3 { get; set; }
    }

    public class ValoracionPlanetaResponseDto
    {
        public int Id_Valoracion { get; set; }
        public int Id_Planeta { get; set; }
        public string Clase_Planeta { get; set; } = null!;
        public decimal Valor_Total { get; set; }
        public decimal Precio_Final { get; set; }
        public decimal Recursos_Score { get; set; }
        public decimal Tecnologia_Score { get; set; }
        public decimal Ubicacion_Score { get; set; }
        public decimal Poder_Score { get; set; }
        public decimal Riesgo_Score { get; set; }
        public string Estado_Valoracion { get; set; } = null!;
        public string Analista { get; set; } = null!;
        public string? Aprobado_Por { get; set; }
        public DateTime Fecha_Valoracion { get; set; }
        public DateTime? Fecha_Aprobacion { get; set; }
        public string? Observaciones { get; set; }
        public PlanetaResumenValoracionDto Planeta { get; set; } = null!;
    }
}
