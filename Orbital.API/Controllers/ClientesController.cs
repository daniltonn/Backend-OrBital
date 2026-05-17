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
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _service;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(IClienteService service, ILogger<ClientesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =========================
        // GET - LISTAR CLIENTES (administrador)
        // =========================
        [Authorize(Policy = Policies.ClientesAdministrar)]
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] string? tipo,
            [FromQuery] string? nivelConfianza,
            [FromQuery] bool? activo)
        {
            try
            {
                var clientes = await _service.Listar(tipo, nivelConfianza, activo);

                return Ok(new
                {
                    message = "Clientes obtenidos exitosamente",
                    cantidad = clientes.Count,
                    filtros = new { tipo, nivelConfianza, activo },
                    data = clientes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar clientes");
                return StatusCode(500, new { message = "Error interno al listar clientes", error = ex.Message });
            }
        }

        // =========================
        // GET - PERFIL DE CLIENTE CON COMPRAS
        // =========================
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPerfil(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de cliente inválido" });

                var esCliente = User.FindFirstValue("tipo") == "cliente";
                if (esCliente)
                {
                    var idTokenCliente = ObtenerIdCliente();
                    if (idTokenCliente != id)
                        return Forbid();
                }

                var perfil = await _service.ObtenerDetalleConCompras(id);

                if (perfil == null)
                    return NotFound(new { message = "Cliente no encontrado" });

                return Ok(new
                {
                    message = "Perfil de cliente obtenido exitosamente",
                    data = perfil
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil del cliente {Id}", id);
                return StatusCode(500, new { message = "Error interno al obtener perfil", error = ex.Message });
            }
        }

        // =========================
        // POST - CREAR CLIENTE (administrador)
        // =========================
        [Authorize(Policy = Policies.ClientesAdministrar)]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ClienteCreateAdminDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return BadRequest(new { message = "El nombre es requerido" });

                if (string.IsNullOrWhiteSpace(dto.Correo))
                    return BadRequest(new { message = "El correo es requerido" });

                if (string.IsNullOrWhiteSpace(dto.Password))
                    return BadRequest(new { message = "La contraseña es requerida" });

                var resultado = await _service.Crear(dto);

                return Ok(new
                {
                    message = "Cliente creado exitosamente",
                    data = resultado
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente");
                return StatusCode(500, new { message = "Error interno al crear cliente", error = ex.Message });
            }
        }

        // =========================
        // PATCH - EDITAR CLIENTE PARCIALMENTE (administrador)
        // =========================
        [Authorize(Policy = Policies.ClientesAdministrar)]
        [HttpPatch("{id}")]
        public async Task<IActionResult> EditarParcial(int id, [FromBody] ClienteUpdateDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de cliente inválido" });

                var idUsuario = ObtenerIdUsuarioInterno();
                var ip = ObtenerIp();
                var resultado = await _service.Actualizar(id, dto, idUsuario, ip);

                return Ok(new
                {
                    message = "Datos del cliente actualizados exitosamente",
                    data = resultado
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar cliente {Id}", id);
                return StatusCode(500, new { message = "Error interno al editar cliente", error = ex.Message });
            }
        }

        // =========================
        // PUT - EDITAR DATOS DEL CLIENTE (administrador)
        // =========================
        [Authorize(Policy = Policies.ClientesAdministrar)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] ClienteUpdateDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de cliente inválido" });

                var idUsuario = ObtenerIdUsuarioInterno();
                var ip = ObtenerIp();
                var resultado = await _service.Actualizar(id, dto, idUsuario, ip);

                return Ok(new
                {
                    message = "Datos del cliente actualizados exitosamente",
                    data = resultado
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar cliente {Id}", id);
                return StatusCode(500, new { message = "Error interno al editar cliente", error = ex.Message });
            }
        }

        // =========================
        // PATCH - AJUSTAR CRÉDITO (administrador)
        // =========================
        [Authorize(Policy = Policies.ClientesAdministrar)]
        [HttpPatch("{id}/credito")]
        public async Task<IActionResult> AjustarCredito(int id, [FromBody] CreditoAjusteDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "ID de cliente inválido" });

                if (dto.Monto == 0)
                    return BadRequest(new { message = "El monto no puede ser cero" });

                if (string.IsNullOrWhiteSpace(dto.Motivo))
                    return BadRequest(new { message = "El motivo del ajuste es requerido" });

                var idUsuario = ObtenerIdUsuarioInterno();
                var ip = ObtenerIp();
                var resultado = await _service.AjustarCredito(id, dto, idUsuario, ip);

                var accion = dto.Monto > 0 ? "Recarga" : "Descuento";
                return Ok(new
                {
                    message = $"{accion} de crédito aplicado exitosamente",
                    data = resultado
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ajustar crédito del cliente {Id}", id);
                return StatusCode(500, new { message = "Error interno al ajustar crédito", error = ex.Message });
            }
        }

        private int ObtenerIdCliente()
        {
            var claim = User.FindFirstValue("Id_Cliente");
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private int ObtenerIdUsuarioInterno()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub") ?? "0";
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private string ObtenerIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
    }
}
