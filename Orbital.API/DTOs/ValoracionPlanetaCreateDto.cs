namespace Orbital.API.DTOs
{
    public class ValoracionPlanetaCreateDto
    {
        public int Id_Planeta { get; set; }
        public string? Observaciones { get; set; }
        public decimal? Recursos_Score { get; set; }
        public decimal? Tecnologia_Score { get; set; }
        public decimal? Ubicacion_Score { get; set; }
        public decimal? Poder_Score { get; set; }
        public decimal? Riesgo_Score { get; set; }
    }
}
