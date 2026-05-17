using Microsoft.AspNetCore.Mvc;
using Orbital.API.DTOs;
using Orbital.API.Services;
using Microsoft.AspNetCore.Authorization;
using Orbital.API.Authorization;
using System.Security.Claims;

namespace Orbital.API.Controllers
{
    [ApiController]
    [Route("api/Valoraciones")]
    public class ValoracionPlanetasController : ControllerBase
    {
        private readonly IValoracionService _service;
        private readonly ILogger<ValoracionPlanetasController> _logger;

        public ValoracionPlanetasController(
            IValoracionService service,
            ILogger<ValoracionPlanetasController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =========================
        // POST - EVALUAR PLANETA
        // =========================
        /// <summary>
        /// Calcula y almacena la evaluación estratégica de un planeta
        /// </summary>
        [Authorize(Policy = Policies.ValoracionesCreate)]
        [HttpPost]
        public async Task<IActionResult> EvaluarPlaneta([FromBody] ValoracionPlanetaCreateDto dto)
        {
            try
            {
                if (dto.Id_Planeta <= 0)
                    return BadRequest(new
                    {
                        message = "ID del planeta debe ser válido",
                        error = "Parámetros inválidos"
                    });

                var analistaId = ObtenerIdUsuario();
                if (analistaId <= 0)
                    return Unauthorized(new { message = "No se pudo obtener el analista del token" });

                var resultado = await _service.CalcularValorEstrategico(
                    dto.Id_Planeta,
                    analistaId,
                    dto.Observaciones,
                    dto.Recursos_Score,
                    dto.Tecnologia_Score,
                    dto.Ubicacion_Score,
                    dto.Poder_Score,
                    dto.Riesgo_Score);

                return CreatedAtAction(
                    nameof(ObtenerValoracionPorId),
                    new { id = resultado.Id_Valoracion },
                    new
                    {
                        message = "Evaluación estratégica calculada y guardada exitosamente",
                        data = resultado
                    });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al evaluar planeta");
                return BadRequest(new
                {
                    message = "Error de validación",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar planeta");
                return StatusCode(500, new
                {
                    message = "Error interno al evaluar planeta",
                    error = ex.Message
                });
            }
        }

        // =========================
        // GET - OBTENER VALORACION POR ID
        // =========================
        /// <summary>
        /// Obtiene una valoración específica por su ID
        /// </summary>
        [Authorize(Policy = Policies.ValoracionesRead)]
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerValoracionPorId(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de valoración inválido" });

                var valoracion = await _service.ObtenerValoracionPorId(id);

                if (valoracion == null)
                    return NotFound(new { message = "Valoración no encontrada" });

                return Ok(new
                {
                    message = "Valoración obtenida exitosamente",
                    data = valoracion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener valoración {Id}", id);
                return StatusCode(500, new
                {
                    message = "Error interno al obtener valoración",
                    error = ex.Message
                });
            }
        }

        // =========================
        // GET - OBTENER VALORACIONES DE UN PLANETA
        // =========================
        /// <summary>
        /// Obtiene todas las valoraciones registradas para un planeta específico
        /// </summary>
        [Authorize(Policy = Policies.ValoracionesRead)]
        [HttpGet("planeta/{planetaId}")]
        public async Task<IActionResult> ObtenerValoracionesPlaneta(int planetaId)
        {
            try
            {
                if (planetaId <= 0)
                    return BadRequest(new { message = "ID del planeta inválido" });

                var valoraciones = await _service.ObtenerValoracionesPlaneta(planetaId);

                return Ok(new
                {
                    message = "Valoraciones obtenidas exitosamente",
                    cantidad = valoraciones.Count,
                    data = valoraciones
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener valoraciones del planeta {PlanetaId}", planetaId);
                return StatusCode(500, new
                {
                    message = "Error interno al obtener valoraciones",
                    error = ex.Message
                });
            }
        }

        // =========================
        // GET - OBTENER FACTORES DE EVALUACION
        // =========================
        /// <summary>
        /// Obtiene los factores desglosados de una valoración
        /// </summary>
        [Authorize(Policy = Policies.ValoracionesRead)]
        [HttpGet("{id}/factores")]
        public async Task<IActionResult> ObtenerFactoresValoracion(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de valoración inválido" });

                var factores = await _service.ObtenerFactoresValoracion(id);

                if (factores == null)
                    return NotFound(new { message = "Valoración no encontrada" });

                return Ok(new
                {
                    message = "Factores de evaluación obtenidos exitosamente",
                    data = factores
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener factores de valoración {Id}", id);
                return StatusCode(500, new
                {
                    message = "Error interno al obtener factores",
                    error = ex.Message
                });
            }
        }

        // =========================
        // GET - LISTAR TODAS LAS VALORACIONES CON FILTROS
        // =========================
        /// <summary>
        /// Obtiene todas las valoraciones con filtros opcionales
        /// </summary>
        [Authorize(Policy = Policies.ValoracionesRead)]
        [HttpGet]
        public async Task<IActionResult> ObtenerTodas(
            [FromQuery] string? estado = null,
            [FromQuery] int? idPlaneta = null,
            [FromQuery] int? planetaId = null,
            [FromQuery] int? analistaId = null)
        {
            try
            {
                var filtroIdPlaneta = idPlaneta ?? planetaId;
                var valoraciones = await _service.ObtenerTodos(estado, filtroIdPlaneta, analistaId);

                return Ok(new
                {
                    message = "Valoraciones obtenidas exitosamente",
                    cantidad = valoraciones.Count,
                    filtros = new { estado, idPlaneta = filtroIdPlaneta, analistaId },
                    data = valoraciones
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las valoraciones");
                return StatusCode(500, new
                {
                    message = "Error interno al obtener valoraciones",
                    error = ex.Message
                });
            }
        }

        // =========================
        // PATCH - APROBAR VALORACION
        // =========================
        /// <summary>
        /// Aprueba una valoración en estado Pendiente
        /// </summary>
        [Authorize(Policy = Policies.ValoracionesApprove)]
        [HttpPatch("{id}/aprobar")]
        public async Task<IActionResult> AprobarRechazarValoracion(int id, [FromBody] AprobarRechazarValoracionDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de valoración inválido" });

                var aprobadorId = ObtenerIdUsuario();
                if (aprobadorId <= 0)
                    return Unauthorized(new { message = "No se pudo obtener el aprobador del token" });

                if (dto.Aprobado)
                {
                    var resultado = await _service.AprobarValoracion(id, aprobadorId);
                    if (resultado == null)
                        return NotFound(new { message = "Valoración no encontrada" });

                    return Ok(new { message = "Valoración aprobada exitosamente", data = resultado });
                }
                else
                {
                    var motivo = dto.Observaciones ?? "Sin motivo especificado";
                    var rechazada = await _service.RechazarValoracion(id, motivo);
                    if (!rechazada)
                        return NotFound(new { message = "Valoración no encontrada" });

                    return Ok(new
                    {
                        message = "Valoración rechazada exitosamente",
                        data = new { Id_Valoracion = id, Estado = "Rechazada", Motivo = motivo }
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de operación al aprobar/rechazar valoración {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al aprobar/rechazar valoración {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar/rechazar valoración {Id}", id);
                return StatusCode(500, new { message = "Error interno al procesar valoración", error = ex.Message });
            }
        }

        // =========================
        // PATCH - RECHAZAR VALORACION
        // =========================
        /// <summary>
        /// Rechaza una valoración en estado Pendiente
        /// </summary>
        [Authorize(Policy = Policies.ValoracionesReject)]
        [HttpPatch("{id}/rechazar")]
        public async Task<IActionResult> RechazarValoracion(int id, [FromBody] RechazarValoracionDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de valoración inválido" });

                if (string.IsNullOrWhiteSpace(dto.Motivo))
                    return BadRequest(new { message = "El motivo del rechazo es requerido" });

                var resultado = await _service.RechazarValoracion(id, dto.Motivo);

                if (!resultado)
                    return NotFound(new { message = "Valoración no encontrada" });

                return Ok(new
                {
                    message = "Valoración rechazada exitosamente",
                    data = new { Id_Valoracion = id, Estado = "Rechazada", Motivo = dto.Motivo }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de operación al rechazar valoración {Id}", id);
                return BadRequest(new
                {
                    message = "No se puede rechazar",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar valoración {Id}", id);
                return StatusCode(500, new
                {
                    message = "Error interno al rechazar valoración",
                    error = ex.Message
                });
            }
        }

        private int ObtenerIdUsuario()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub") ?? "0";
            return int.TryParse(idClaim, out var id) ? id : 0;
        }
    }

    // =========================
    // DTOs AUXILIARES PARA ENDPOINTS
    // =========================
    public class AprobarRechazarValoracionDto
    {
        public bool Aprobado { get; set; }
        public string? Observaciones { get; set; }
    }

    public class RechazarValoracionDto
    {
        public string Motivo { get; set; } = null!;
    }
}
