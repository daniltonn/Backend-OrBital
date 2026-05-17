using Orbital.API.DTOs;

namespace Orbital.API.Services
{
    public interface IValoracionService
    {
        /// <summary>
        /// Calcula y almacena la valoración estratégica de un planeta
        /// RF-3.4.2.4 y RF-3.4.2.5
        /// </summary>
        Task<ValoracionPlanetaResponseDto> CalcularValorEstrategico(
            int planetaId,
            int analistaId,
            string? observaciones = null,
            decimal? recursosScore = null,
            decimal? tecnologiaScore = null,
            decimal? ubicacionScore = null,
            decimal? poderScore = null,
            decimal? riesgoScore = null);

        /// <summary>
        /// Obtiene todas las valoraciones de un planeta específico
        /// RF-3.4.2.6 y RF-3.4.2.8
        /// </summary>
        Task<List<ValoracionPlanetaResponseDto>> ObtenerValoracionesPlaneta(int planetaId);

        /// <summary>
        /// Obtiene una valoración específica por su ID
        /// </summary>
        Task<ValoracionPlanetaResponseDto?> ObtenerValoracionPorId(int valoracionId);

        /// <summary>
        /// Obtiene los factores desglosados de una valoración
        /// RF-3.4.2.7
        /// </summary>
        Task<FactoresValoracionDto?> ObtenerFactoresValoracion(int valoracionId);

        /// <summary>
        /// Obtiene todas las valoraciones con filtros opcionales
        /// </summary>
        Task<List<ValoracionPlanetaResponseDto>> ObtenerTodos(
            string? estado = null,
            int? planetaId = null,
            int? analistaId = null);

        /// <summary>
        /// Aprueba una valoración pendiente
        /// </summary>
        Task<ValoracionPlanetaResponseDto?> AprobarValoracion(int valoracionId, int aprobadorId);

        /// <summary>
        /// Rechaza una valoración pendiente
        /// </summary>
        Task<bool> RechazarValoracion(int valoracionId, string motivo);
    }
}
