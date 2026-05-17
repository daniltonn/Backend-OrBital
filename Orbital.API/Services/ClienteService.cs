using BCrypt.Net;
using Orbital.API.Data;
using Orbital.API.DTOs;
using Orbital.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Orbital.API.Services
{
    public class ClienteService : IClienteService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ClienteService> _logger;

        public ClienteService(AppDbContext context, ILogger<ClienteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ClienteResponseDto>> Listar(string? tipo, string? nivelConfianza, bool? activo)
        {
            var query = _context.Clientes
                .Include(c => c.GalaxiaOrigen)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tipo))
                query = query.Where(c => c.Tipo_Cliente == tipo);

            if (!string.IsNullOrEmpty(nivelConfianza))
                query = query.Where(c => c.Nivel_Confianza == nivelConfianza);

            if (activo.HasValue)
                query = query.Where(c => c.Activo == activo.Value);

            var clientes = await query.OrderBy(c => c.Nombre).ToListAsync();
            return clientes.Select(MapearDto).ToList();
        }

        public async Task<ClienteResponseDto?> ObtenerPorId(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.GalaxiaOrigen)
                .FirstOrDefaultAsync(c => c.Id_Cliente == id && c.Activo);

            return cliente == null ? null : MapearDto(cliente);
        }

        public async Task<ClienteDetalleDto?> ObtenerDetalleConCompras(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.GalaxiaOrigen)
                .FirstOrDefaultAsync(c => c.Id_Cliente == id);

            if (cliente == null) return null;

            var compras = await _context.Transacciones
                .Where(t => t.Id_Comprador == id)
                .Join(
                    _context.MercadoPlanetas.Include(m => m.Planeta),
                    t => t.Id_Publicacion,
                    m => m.Id_Publicacion,
                    (t, m) => new TransaccionClienteDto
                    {
                        Id_Transaccion = t.Id_Transaccion,
                        Nombre_Planeta = m.Planeta != null ? m.Planeta.Nombre : "Desconocido",
                        Precio_Final = t.Precio_Final,
                        Fecha_Transaccion = t.Fecha_Transaccion,
                        Estado_Transaccion = t.Estado_Transaccion
                    }
                )
                .OrderByDescending(t => t.Fecha_Transaccion)
                .ToListAsync();

            var base_ = MapearDto(cliente);
            return new ClienteDetalleDto
            {
                Id_Cliente = base_.Id_Cliente,
                Nombre = base_.Nombre,
                Tipo_Cliente = base_.Tipo_Cliente,
                Galaxia_Origen = base_.Galaxia_Origen,
                Correo = base_.Correo,
                Credito_Disponible = base_.Credito_Disponible,
                Nivel_Confianza = base_.Nivel_Confianza,
                Fecha_Registro = base_.Fecha_Registro,
                Activo = base_.Activo,
                Compras = compras
            };
        }

        public async Task<ClienteResponseDto> Crear(ClienteCreateAdminDto dto)
        {
            var existe = await _context.Clientes.AnyAsync(c => c.Correo == dto.Correo.Trim().ToLower());
            if (existe)
                throw new InvalidOperationException("Ya existe un cliente con ese correo");

            var cliente = new Cliente
            {
                Nombre = dto.Nombre,
                Tipo_Cliente = dto.Tipo_Cliente,
                Id_Galaxia_Origen = dto.Id_Galaxia_Origen,
                Correo = dto.Correo.Trim().ToLower(),
                Contrasena_Hash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Credito_Disponible = dto.Credito_Disponible,
                Nivel_Confianza = dto.Nivel_Confianza,
                Fecha_Registro = DateTime.Now,
                Activo = true
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            if (cliente.Id_Galaxia_Origen.HasValue)
                await _context.Entry(cliente).Reference(c => c.GalaxiaOrigen).LoadAsync();

            return MapearDto(cliente);
        }

        public async Task<ClienteResponseDto> Actualizar(int id, ClienteUpdateDto dto, int idUsuario, string ipOrigen)
        {
            var cliente = await _context.Clientes
                .Include(c => c.GalaxiaOrigen)
                .FirstOrDefaultAsync(c => c.Id_Cliente == id && c.Activo);

            if (cliente == null)
                throw new KeyNotFoundException("Cliente no encontrado");

            var valorAnterior = JsonSerializer.Serialize(new
            {
                cliente.Nombre, cliente.Tipo_Cliente,
                cliente.Id_Galaxia_Origen, cliente.Correo, cliente.Nivel_Confianza
            });

            if (dto.Nombre != null) cliente.Nombre = dto.Nombre;
            if (dto.Tipo_Cliente != null) cliente.Tipo_Cliente = dto.Tipo_Cliente;
            if (dto.Id_Galaxia_Origen.HasValue) cliente.Id_Galaxia_Origen = dto.Id_Galaxia_Origen;
            if (dto.Correo != null) cliente.Correo = dto.Correo.Trim().ToLower();
            if (dto.Nivel_Confianza != null) cliente.Nivel_Confianza = dto.Nivel_Confianza;

            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();

            // Recargar galaxia si cambió
            await _context.Entry(cliente).Reference(c => c.GalaxiaOrigen).LoadAsync();

            await RegistrarAuditoria(
                idUsuario, "EDITAR_CLIENTE", "cliente",
                cliente.Id_Cliente, valorAnterior,
                JsonSerializer.Serialize(new
                {
                    cliente.Nombre, cliente.Tipo_Cliente,
                    cliente.Id_Galaxia_Origen, cliente.Correo, cliente.Nivel_Confianza
                }),
                ipOrigen);

            return MapearDto(cliente);
        }

        public async Task<ClienteResponseDto> AjustarCredito(int id, CreditoAjusteDto dto, int idUsuario, string ipOrigen)
        {
            var cliente = await _context.Clientes
                .Include(c => c.GalaxiaOrigen)
                .FirstOrDefaultAsync(c => c.Id_Cliente == id && c.Activo);

            if (cliente == null)
                throw new KeyNotFoundException("Cliente no encontrado");

            var creditoAnterior = cliente.Credito_Disponible;
            var creditoNuevo = creditoAnterior + dto.Monto;

            if (creditoNuevo < 0)
                throw new InvalidOperationException(
                    $"El crédito resultante sería negativo ({creditoNuevo}). Crédito actual: {creditoAnterior}");

            cliente.Credito_Disponible = creditoNuevo;
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();

            await RegistrarAuditoria(
                idUsuario,
                $"AJUSTE_CREDITO. Motivo: {dto.Motivo}",
                "cliente",
                cliente.Id_Cliente,
                creditoAnterior.ToString("F2"),
                creditoNuevo.ToString("F2"),
                ipOrigen);

            return MapearDto(cliente);
        }

        private static ClienteResponseDto MapearDto(Cliente c) => new()
        {
            Id_Cliente = c.Id_Cliente,
            Nombre = c.Nombre,
            Tipo_Cliente = c.Tipo_Cliente,
            Galaxia_Origen = c.GalaxiaOrigen?.Nombre,
            Correo = c.Correo,
            Credito_Disponible = c.Credito_Disponible,
            Nivel_Confianza = c.Nivel_Confianza,
            Fecha_Registro = c.Fecha_Registro,
            Activo = c.Activo
        };

        private async Task RegistrarAuditoria(
            int? idUsuario, string accion, string tabla,
            int idRegistro, string? valorAnterior, string? valorNuevo, string? ipOrigen)
        {
            _context.Auditorias.Add(new Auditoria
            {
                Id_Usuario = idUsuario,
                Accion = accion,
                Tabla_Afectada = tabla,
                Id_Registro_Afectado = idRegistro,
                Valor_Anterior = valorAnterior,
                Valor_Nuevo = valorNuevo,
                Timestamp_Accion = DateTime.UtcNow,
                Ip_Origen = ipOrigen,
                Resultado = "Exitoso"
            });
            await _context.SaveChangesAsync();
        }
    }
}
