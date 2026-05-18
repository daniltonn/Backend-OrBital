using Orbital.API.Data;
using Orbital.API.DTOs;
using Orbital.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Orbital.API.Services
{
    public class TransaccionService : ITransaccionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TransaccionService> _logger;

        public TransaccionService(AppDbContext context, ILogger<TransaccionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static readonly string[] MetodosPagoValidos =
        [
            "Créditos Galácticos",
            "Criptomoneda",
            "Intercambio Territorial",
            "Contrato de Servicios"
        ];

        public async Task<TransaccionListItemDto> ComprarPlaneta(
            int idPublicacion, int idCliente, ComprarPlanetaDto dto, string ipOrigen)
        {
            if (!MetodosPagoValidos.Contains(dto.Metodo_Pago))
                throw new ArgumentException(
                    $"Método de pago inválido. Valores permitidos: {string.Join(", ", MetodosPagoValidos)}");

            var ahora = DateTime.Now;

            var publicacion = await _context.MercadoPlanetas
                .Include(m => m.Planeta)
                .FirstOrDefaultAsync(m => m.Id_Publicacion == idPublicacion && m.Activo);

            if (publicacion == null)
                throw new InvalidOperationException("La publicación no existe o ya no está disponible");

            if (publicacion.Fecha_Vencimiento.HasValue && publicacion.Fecha_Vencimiento < ahora)
                throw new InvalidOperationException("La publicación ha vencido y ya no está disponible");

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id_Cliente == idCliente && c.Activo);

            if (cliente == null)
                throw new KeyNotFoundException("Cliente no encontrado");

            if (cliente.Credito_Disponible < publicacion.Precio_Publicado)
                throw new InvalidOperationException(
                    $"Crédito insuficiente. Disponible: {cliente.Credito_Disponible}, Requerido: {publicacion.Precio_Publicado}");

            Transaccion transaccion;
            using (var dbTransaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    cliente.Credito_Disponible -= publicacion.Precio_Publicado;
                    _context.Clientes.Update(cliente);

                    transaccion = new Transaccion
                    {
                        Id_Publicacion = idPublicacion,
                        Id_Comprador = idCliente,
                        Id_Vendedor = publicacion.Id_Publicado_Por,
                        Precio_Final = publicacion.Precio_Publicado,
                        Fecha_Transaccion = ahora,
                        Estado_Transaccion = "Pendiente",
                        Metodo_Pago = dto.Metodo_Pago,
                        Notas = dto.Notas
                    };

                    _context.Transacciones.Add(transaccion);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }

            // Auditoría fuera de la transacción para no mezclar rollbacks
            await RegistrarAuditoria(
                null, "COMPRA_INICIADA", "transaccion",
                transaccion.Id_Transaccion,
                null,
                JsonSerializer.Serialize(new { idPublicacion, idCliente, transaccion.Precio_Final }),
                ipOrigen);

            return new TransaccionListItemDto
            {
                Id_Transaccion = transaccion.Id_Transaccion,
                Nombre_Planeta = publicacion.Planeta?.Nombre ?? "Desconocido",
                Nombre_Comprador = cliente.Nombre,
                Comprador_Tipo = cliente.Tipo_Cliente,
                Precio_Final = transaccion.Precio_Final,
                Metodo_Pago = transaccion.Metodo_Pago,
                Fecha_Transaccion = transaccion.Fecha_Transaccion,
                Estado_Transaccion = transaccion.Estado_Transaccion,
                Notas = transaccion.Notas
            };
        }

        public async Task<TransaccionListItemDto> CambiarEstadoTransaccion(
            int idTransaccion, CambiarEstadoTransaccionDto dto, int idUsuario, string ipOrigen)
        {
            var estadosValidos = new[] { "Completada", "Anulada", "En Disputa" };
            if (!estadosValidos.Contains(dto.Estado))
                throw new ArgumentException($"Estado inválido. Valores permitidos: {string.Join(", ", estadosValidos)}");

            // Cargar transacción con toda la info necesaria en una sola query
            var transaccion = await _context.Transacciones
                .FirstOrDefaultAsync(t => t.Id_Transaccion == idTransaccion);

            if (transaccion == null)
                throw new KeyNotFoundException("Transacción no encontrada");

            if (transaccion.Estado_Transaccion != "Pendiente")
                throw new InvalidOperationException(
                    $"Solo se puede cambiar el estado de transacciones Pendientes. Estado actual: {transaccion.Estado_Transaccion}");

            var publicacion = await _context.MercadoPlanetas
                .Include(m => m.Planeta)
                .FirstOrDefaultAsync(m => m.Id_Publicacion == transaccion.Id_Publicacion);

            if (publicacion == null)
                throw new KeyNotFoundException("Publicación asociada no encontrada");

            var estadoAnterior = transaccion.Estado_Transaccion;

            if (dto.Estado == "Completada")
            {
                await CompletarTransaccionAtomicamente(transaccion, publicacion, dto.Notas, idUsuario);
            }
            else
            {
                // Anulada o En Disputa
                transaccion.Estado_Transaccion = dto.Estado;
                if (dto.Notas != null) transaccion.Notas = dto.Notas;

                // Si se anula, devolver crédito al cliente
                if (dto.Estado == "Anulada")
                {
                    var cliente = await _context.Clientes
                        .FirstOrDefaultAsync(c => c.Id_Cliente == transaccion.Id_Comprador);

                    if (cliente != null)
                    {
                        cliente.Credito_Disponible += transaccion.Precio_Final;
                        _context.Clientes.Update(cliente);
                    }
                }

                _context.Transacciones.Update(transaccion);
                await _context.SaveChangesAsync();
            }

            await RegistrarAuditoria(
                idUsuario,
                $"CAMBIO_ESTADO_TRANSACCION: {estadoAnterior} → {dto.Estado}",
                "transaccion",
                transaccion.Id_Transaccion,
                estadoAnterior,
                dto.Estado,
                ipOrigen);

            var compradorInfo = await _context.Clientes
                .Where(c => c.Id_Cliente == transaccion.Id_Comprador)
                .Select(c => new { c.Nombre, c.Tipo_Cliente })
                .FirstOrDefaultAsync();

            var vendedorNombre = await _context.Usuarios
                .Where(u => u.Id_Usuario == transaccion.Id_Vendedor)
                .Select(u => u.Nombre)
                .FirstOrDefaultAsync();

            return new TransaccionListItemDto
            {
                Id_Transaccion = transaccion.Id_Transaccion,
                Nombre_Planeta = publicacion.Planeta?.Nombre ?? "Desconocido",
                Nombre_Comprador = compradorInfo?.Nombre ?? transaccion.Id_Comprador.ToString(),
                Comprador_Tipo = compradorInfo?.Tipo_Cliente,
                Vendedor_Nombre = vendedorNombre,
                Precio_Final = transaccion.Precio_Final,
                Metodo_Pago = transaccion.Metodo_Pago,
                Fecha_Transaccion = transaccion.Fecha_Transaccion,
                Estado_Transaccion = transaccion.Estado_Transaccion,
                Notas = transaccion.Notas
            };
        }

        private const int EstadoVendidoId = 6;

        private async Task CompletarTransaccionAtomicamente(
            Transaccion transaccion, MercadoPlaneta publicacion, string? notas, int idUsuario)
        {
            var planeta = await _context.Planetas
                .FirstOrDefaultAsync(p => p.Id_Planeta == publicacion.Id_Planeta);

            if (planeta == null)
                throw new KeyNotFoundException("Planeta asociado a la publicación no encontrado");

            int estadoAnteriorId = planeta.Id_Estado;

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Actualizar estado de la transacción
                transaccion.Estado_Transaccion = "Completada";
                if (notas != null) transaccion.Notas = notas;
                _context.Transacciones.Update(transaccion);

                // 2. Marcar el planeta como Vendido y actualizar propietario
                planeta.Id_Estado = EstadoVendidoId;
                planeta.Id_Propietario = transaccion.Id_Comprador;
                _context.Planetas.Update(planeta);

                // 3. Desactivar la publicación
                publicacion.Activo = false;
                _context.MercadoPlanetas.Update(publicacion);

                // 4. Registrar en histórico con todas las columnas NOT NULL requeridas
                var historico = new HistoricoCicloPlanetario
                {
                    Id_Planeta = publicacion.Id_Planeta,
                    Id_Estado_Anterior = estadoAnteriorId,
                    Id_Estado_Nuevo = EstadoVendidoId,
                    Id_Usuario_Cambio = idUsuario,
                    Id_Transaccion = transaccion.Id_Transaccion,
                    Id_Cliente_Nuevo = transaccion.Id_Comprador,
                    Tipo_Evento = "Venta",
                    Descripcion = $"Planeta vendido por {transaccion.Precio_Final} créditos",
                    Fecha_Cambio = DateTime.Now
                };
                _context.HistoricosCiclo.Add(historico);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation(
                    "Transacción {Id} completada. Planeta {Planet} transferido al cliente {Cliente}",
                    transaccion.Id_Transaccion, publicacion.Id_Planeta, transaccion.Id_Comprador);
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<TransaccionListItemDto>> ListarTransacciones(
            string? estado, DateTime? fechaInicio, DateTime? fechaFin, int? idComprador, int? idPublicacion = null)
        {
            var query = (
                from t in _context.Transacciones
                join m in _context.MercadoPlanetas on t.Id_Publicacion equals m.Id_Publicacion
                join p in _context.Planetas on m.Id_Planeta equals p.Id_Planeta into planetaGroup
                from planeta in planetaGroup.DefaultIfEmpty()
                join c in _context.Clientes on t.Id_Comprador equals c.Id_Cliente into compradorGroup
                from comprador in compradorGroup.DefaultIfEmpty()
                join u in _context.Usuarios on t.Id_Vendedor equals u.Id_Usuario into vendedorGroup
                from vendedor in vendedorGroup.DefaultIfEmpty()
                select new
                {
                    t.Id_Transaccion,
                    t.Id_Publicacion,
                    t.Id_Comprador,
                    t.Id_Vendedor,
                    t.Precio_Final,
                    t.Fecha_Transaccion,
                    t.Estado_Transaccion,
                    t.Metodo_Pago,
                    t.Notas,
                    NombrePlaneta = planeta != null ? planeta.Nombre : "Desconocido",
                    NombreComprador = comprador != null ? comprador.Nombre : t.Id_Comprador.ToString(),
                    CompradorTipo = comprador != null ? comprador.Tipo_Cliente : null,
                    NombreVendedor = vendedor != null ? vendedor.Nombre : null
                }
            ).AsQueryable();

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(x => x.Estado_Transaccion == estado);

            if (fechaInicio.HasValue)
                query = query.Where(x => x.Fecha_Transaccion >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(x => x.Fecha_Transaccion <= fechaFin.Value);

            if (idComprador.HasValue)
                query = query.Where(x => x.Id_Comprador == idComprador.Value);

            if (idPublicacion.HasValue)
                query = query.Where(x => x.Id_Publicacion == idPublicacion.Value);

            var resultados = await query
                .OrderByDescending(x => x.Fecha_Transaccion)
                .ToListAsync();

            return resultados.Select(x => new TransaccionListItemDto
            {
                Id_Transaccion = x.Id_Transaccion,
                Nombre_Planeta = x.NombrePlaneta,
                Nombre_Comprador = x.NombreComprador,
                Comprador_Tipo = x.CompradorTipo,
                Vendedor_Nombre = x.NombreVendedor,
                Precio_Final = x.Precio_Final,
                Metodo_Pago = x.Metodo_Pago,
                Fecha_Transaccion = x.Fecha_Transaccion,
                Estado_Transaccion = x.Estado_Transaccion,
                Notas = x.Notas
            }).ToList();
        }

        public async Task<List<TransaccionClienteDto>> ListarComprasCliente(int idCliente)
        {
            var transacciones = await _context.Transacciones
                .Where(t => t.Id_Comprador == idCliente)
                .Join(
                    _context.MercadoPlanetas.Include(m => m.Planeta),
                    t => t.Id_Publicacion,
                    m => m.Id_Publicacion,
                    (t, m) => new { Transaccion = t, Publicacion = m }
                )
                .OrderByDescending(x => x.Transaccion.Fecha_Transaccion)
                .ToListAsync();

            return transacciones.Select(x => new TransaccionClienteDto
            {
                Id_Transaccion = x.Transaccion.Id_Transaccion,
                Nombre_Planeta = x.Publicacion.Planeta?.Nombre ?? "Desconocido",
                Precio_Final = x.Transaccion.Precio_Final,
                Fecha_Transaccion = x.Transaccion.Fecha_Transaccion,
                Estado_Transaccion = x.Transaccion.Estado_Transaccion
            }).ToList();
        }

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
