using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbital.API.Authorization;
using Orbital.API.DTOs;
using Orbital.API.Services;
using System.Security.Claims;

namespace Orbital.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MercadoController : ControllerBase
    {
        private readonly IMercadoService _service;
        private readonly ITransaccionService _transaccionService;
        private readonly ILogger<MercadoController> _logger;

        public MercadoController(
            IMercadoService service,
            ITransaccionService transaccionService,
            ILogger<MercadoController> logger)
        {
            _service = service;
            _transaccionService = transaccionService;
            _logger = logger;
        }

        // =========================
        // GET - LISTAR PLANETAS EN VENTA (público)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] decimal? precioMin,
            [FromQuery] decimal? precioMax,
            [FromQuery] string? clase,
            [FromQuery] int? galaxiaId,
            [FromQuery] bool? activo)
        {
            try
            {
                var lista = await _service.ListarPlanetasEnVenta(precioMin, precioMax, clase, galaxiaId, activo);

                return Ok(new
                {
                    message = "Planetas en venta obtenidos exitosamente",
                    cantidad = lista.Count,
                    filtros = new { precioMin, precioMax, clase, galaxiaId, activo },
                    data = lista
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar planetas en venta");
                return StatusCode(500, new { message = "Error interno al listar planetas en venta", error = ex.Message });
            }
        }

        // =========================
        // GET - DETALLE DE PUBLICACIÓN (público)
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> Detalle(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de publicación inválido" });

                var detalle = await _service.ObtenerDetalle(id);

                if (detalle == null)
                    return NotFound(new { message = "Publicación no encontrada o no está activa" });

                return Ok(new
                {
                    message = "Detalle de publicación obtenido exitosamente",
                    data = detalle
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalle de publicación {Id}", id);
                return StatusCode(500, new { message = "Error interno al obtener detalle", error = ex.Message });
            }
        }

        // =========================
        // POST - PUBLICAR PLANETA (gestor/comandante)
        // =========================
        [Authorize(Policy = Policies.MercadoPublicar)]
        [HttpPost]
        public async Task<IActionResult> Publicar([FromBody] PublicarPlanetaDto dto)
        {
            try
            {
                if (dto.Id_Planeta <= 0 || dto.Id_Valoracion <= 0)
                    return BadRequest(new { message = "Id_Planeta e Id_Valoracion son requeridos" });

                if (dto.Precio_Publicado <= 0 || dto.Precio_Minimo <= 0)
                    return BadRequest(new { message = "Los precios deben ser mayores a cero" });

                var idUsuario = ObtenerIdUsuario();
                var ip = ObtenerIp();
                var resultado = await _service.PublicarPlaneta(dto, idUsuario, ip);

                return CreatedAtAction(nameof(Detalle), new { id = resultado.Id_Publicacion }, new
                {
                    message = "Planeta publicado en el mercado exitosamente",
                    data = resultado
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validación al publicar planeta");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al publicar planeta");
                return StatusCode(500, new { message = "Error interno al publicar planeta", error = ex.Message });
            }
        }

        // =========================
        // PUT - EDITAR PUBLICACIÓN (gestor/comandante)
        // =========================
        [Authorize(Policy = Policies.MercadoEditar)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] EditarPublicacionDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de publicación inválido" });

                var idUsuario = ObtenerIdUsuario();
                var ip = ObtenerIp();
                var resultado = await _service.EditarPublicacion(id, dto, idUsuario, ip);

                return Ok(new
                {
                    message = "Publicación actualizada exitosamente",
                    data = resultado
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar publicación {Id}", id);
                return StatusCode(500, new { message = "Error interno al editar publicación", error = ex.Message });
            }
        }

        // =========================
        // PATCH - RETIRAR PLANETA DEL MERCADO (gestor/comandante/emperador)
        // =========================
        [Authorize(Policy = Policies.MercadoRetirar)]
        [HttpPatch("{id}/retirar")]
        public async Task<IActionResult> Retirar(int id, [FromBody] RetirarMercadoDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de publicación inválido" });

                if (string.IsNullOrWhiteSpace(dto.Motivo))
                    return BadRequest(new { message = "El motivo de retiro es requerido" });

                var idUsuario = ObtenerIdUsuario();
                var ip = ObtenerIp();
                await _service.RetirarPlaneta(id, dto, idUsuario, ip);

                return Ok(new { message = "Planeta retirado del mercado exitosamente" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al retirar planeta del mercado {Id}", id);
                return StatusCode(500, new { message = "Error interno al retirar planeta", error = ex.Message });
            }
        }

        // =========================
        // PATCH - EDITAR PUBLICACIÓN PARCIALMENTE (gestor/comandante)
        // =========================
        [Authorize(Policy = Policies.MercadoEditar)]
        [HttpPatch("{id}")]
        public async Task<IActionResult> EditarParcial(int id, [FromBody] EditarPublicacionDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de publicación inválido" });

                var idUsuario = ObtenerIdUsuario();
                var ip = ObtenerIp();
                var resultado = await _service.EditarPublicacion(id, dto, idUsuario, ip);

                return Ok(new
                {
                    message = "Publicación actualizada exitosamente",
                    data = resultado
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar publicación {Id}", id);
                return StatusCode(500, new { message = "Error interno al editar publicación", error = ex.Message });
            }
        }

        // =========================
        // DELETE - ELIMINAR PUBLICACIÓN (gestor/comandante/emperador)
        // =========================
        [Authorize(Policy = Policies.MercadoRetirar)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de publicación inválido" });

                var idUsuario = ObtenerIdUsuario();
                var ip = ObtenerIp();
                await _service.EliminarPublicacion(id, idUsuario, ip);

                return Ok(new { message = "Publicación eliminada y planeta revertido a estado Disponible" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar publicación {Id}", id);
                return StatusCode(500, new { message = "Error interno al eliminar publicación", error = ex.Message });
            }
        }

        // =========================
        // GET - LISTAR TRANSACCIONES (gestor interno)
        // =========================
        [Authorize(Policy = Policies.TransaccionesLeer)]
        [HttpGet("transacciones")]
        public async Task<IActionResult> ListarTransacciones(
            [FromQuery] string? estado,
            [FromQuery] int? idPublicacion)
        {
            try
            {
                var lista = await _transaccionService.ListarTransacciones(estado, null, null, null, idPublicacion);

                return Ok(new
                {
                    message = "Transacciones obtenidas exitosamente",
                    cantidad = lista.Count,
                    filtros = new { estado, idPublicacion },
                    data = lista
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar transacciones del mercado");
                return StatusCode(500, new { message = "Error interno al listar transacciones", error = ex.Message });
            }
        }

        // =========================
        // POST - COMPRAR PLANETA (cliente autenticado)
        // =========================
        [Authorize(Policy = Policies.ClienteAutenticado)]
        [HttpPost("{id}/comprar")]
        public async Task<IActionResult> Comprar(int id, [FromBody] ComprarPlanetaDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de publicación inválido" });

                if (string.IsNullOrWhiteSpace(dto.Metodo_Pago))
                    return BadRequest(new { message = "El método de pago es requerido" });

                var idCliente = ObtenerIdCliente();
                if (idCliente <= 0)
                    return Unauthorized(new { message = "Token de cliente inválido" });

                var ip = ObtenerIp();
                var resultado = await _transaccionService.ComprarPlaneta(id, idCliente, dto, ip);

                return Ok(new
                {
                    message = "Compra registrada exitosamente. Estado: Pendiente",
                    data = resultado
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error al procesar compra de publicación {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al comprar planeta publicación {Id}", id);
                return StatusCode(500, new { message = "Error interno al procesar compra", error = ex.Message });
            }
        }

        // =========================
        // PATCH - CAMBIAR ESTADO TRANSACCIÓN (gestor interno)
        // =========================
        [Authorize(Policy = Policies.TransaccionesGestionar)]
        [HttpPatch("transacciones/{id}")]
        public async Task<IActionResult> CambiarEstadoTransaccion(int id, [FromBody] CambiarEstadoTransaccionDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de transacción inválido" });

                if (string.IsNullOrWhiteSpace(dto.Estado))
                    return BadRequest(new { message = "El estado es requerido" });

                var idUsuario = ObtenerIdUsuario();
                var ip = ObtenerIp();
                var resultado = await _transaccionService.CambiarEstadoTransaccion(id, dto, idUsuario, ip);

                return Ok(new
                {
                    message = $"Estado de transacción actualizado a '{dto.Estado}' exitosamente",
                    data = resultado
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validación al cambiar estado transacción {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado transacción {Id}", id);
                return StatusCode(500, new { message = "Error interno al cambiar estado", error = ex.Message });
            }
        }

        private int ObtenerIdUsuario()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "0";
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        private int ObtenerIdCliente()
        {
            var claim = User.FindFirstValue("Id_Cliente");
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private string ObtenerIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
    }
}
