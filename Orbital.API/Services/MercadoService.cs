using Orbital.API.Data;
using Orbital.API.DTOs;
using Orbital.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Orbital.API.Services
{
    public class MercadoService : IMercadoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MercadoService> _logger;

        public MercadoService(AppDbContext context, ILogger<MercadoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<MercadoListItemDto>> ListarPlanetasEnVenta(
            decimal? precioMin, decimal? precioMax, string? clase, int? galaxiaId, bool? activo = null)
        {
            var ahora = DateTime.Now;

            var query = _context.MercadoPlanetas
                .Include(m => m.Planeta)
                    .ThenInclude(p => p!.GalaxiaNav)
                .Include(m => m.Planeta)
                    .ThenInclude(p => p!.Estado)
                .Include(m => m.Valoracion)
                .AsQueryable();

            if (activo.HasValue)
                query = query.Where(m => m.Activo == activo.Value);
            else
                query = query.Where(m => m.Activo && (m.Fecha_Vencimiento == null || m.Fecha_Vencimiento > ahora));

            if (precioMin.HasValue)
                query = query.Where(m => m.Precio_Publicado >= precioMin.Value);

            if (precioMax.HasValue)
                query = query.Where(m => m.Precio_Publicado <= precioMax.Value);

            if (!string.IsNullOrEmpty(clase))
                query = query.Where(m => m.Valoracion!.Clase_Planeta == clase);

            if (galaxiaId.HasValue)
                query = query.Where(m => m.Planeta!.Id_Galaxia == galaxiaId.Value);

            var publicaciones = await query
                .OrderByDescending(m => m.Fecha_Publicacion)
                .ToListAsync();

            return publicaciones.Select(m => new MercadoListItemDto
            {
                Id_Publicacion = m.Id_Publicacion,
                Id_Planeta = m.Id_Planeta,
                Nombre_Planeta = m.Planeta?.Nombre ?? "Desconocido",
                Galaxia = m.Planeta?.GalaxiaNav?.Nombre ?? "Desconocida",
                Color1 = m.Planeta?.Color1,
                Color2 = m.Planeta?.Color2,
                Color3 = m.Planeta?.Color3,
                Estado_Planeta = m.Planeta?.Estado?.Nombre ?? "Desconocido",
                Precio_Publicado = m.Precio_Publicado,
                Clase_Planeta = m.Valoracion?.Clase_Planeta ?? "D",
                Valor_Total = m.Valoracion?.Valor_Total ?? 0,
                Precio_Final_Valoracion = m.Valoracion?.Precio_Final ?? 0,
                Descripcion_Venta = m.Descripcion_Venta,
                Fecha_Vencimiento = m.Fecha_Vencimiento,
                Fecha_Publicacion = m.Fecha_Publicacion
            }).ToList();
        }

        public async Task<MercadoDetalleDto?> ObtenerDetalle(int id)
        {
            var publicacion = await _context.MercadoPlanetas
                .Include(m => m.Planeta)
                    .ThenInclude(p => p!.AtmosferaNav)
                .Include(m => m.Planeta)
                    .ThenInclude(p => p!.Coordenadas)
                .Include(m => m.Planeta)
                    .ThenInclude(p => p!.Recursos)
                        .ThenInclude(r => r.Recurso)
                .Include(m => m.Valoracion)
                .Where(m => m.Id_Publicacion == id && m.Activo)
                .FirstOrDefaultAsync();

            if (publicacion == null) return null;

            return new MercadoDetalleDto
            {
                Id_Publicacion = publicacion.Id_Publicacion,
                Id_Planeta = publicacion.Id_Planeta,
                Nombre_Planeta = publicacion.Planeta?.Nombre ?? "Desconocido",
                Descripcion_Planeta = publicacion.Planeta?.Descripcion,
                Descripcion_Venta = publicacion.Descripcion_Venta,
                Precio_Publicado = publicacion.Precio_Publicado,
                Fecha_Vencimiento = publicacion.Fecha_Vencimiento,
                Fecha_Publicacion = publicacion.Fecha_Publicacion,
                Recursos_Score = publicacion.Valoracion?.Recursos_Score ?? 0,
                Tecnologia_Score = publicacion.Valoracion?.Tecnologia_Score ?? 0,
                Ubicacion_Score = publicacion.Valoracion?.Ubicacion_Score ?? 0,
                Poder_Score = publicacion.Valoracion?.Poder_Score ?? 0,
                Riesgo_Score = publicacion.Valoracion?.Riesgo_Score ?? 0,
                Clase_Planeta = publicacion.Valoracion?.Clase_Planeta ?? "D",
                Recursos = publicacion.Planeta?.Recursos
                    .Select(r => new RecursoPlanetaMercadoDto
                    {
                        Nombre_Recurso = r.Recurso?.Nombre ?? "Desconocido",
                        Cantidad_Estimada = r.Cantidad_Estimada,
                        Rareza = r.Recurso?.Rareza ?? "Común"
                    }).ToList() ?? new(),
                Coordenadas = publicacion.Planeta?.Coordenadas == null ? null : new CoordenadasMercadoDto
                {
                    Coordenada_X = publicacion.Planeta.Coordenadas.Coordenada_X,
                    Coordenada_Y = publicacion.Planeta.Coordenadas.Coordenada_Y,
                    Coordenada_Z = publicacion.Planeta.Coordenadas.Coordenada_Z
                },
                Tipo_Atmosfera = publicacion.Planeta?.AtmosferaNav?.Nombre,
                Descripcion_Atmosfera = publicacion.Planeta?.AtmosferaNav?.Descripcion
            };
        }

        public async Task<MercadoListItemDto> PublicarPlaneta(PublicarPlanetaDto dto, int idUsuario, string ipOrigen)
        {
            var planeta = await _context.Planetas
                .Include(p => p.GalaxiaNav)
                .FirstOrDefaultAsync(p => p.Id_Planeta == dto.Id_Planeta && p.Activo);

            if (planeta == null)
                throw new KeyNotFoundException("Planeta no encontrado o inactivo");

            // Verificar que el planeta esté conquistado
            if (string.IsNullOrEmpty(planeta.Conquistado_Por))
                throw new InvalidOperationException(
                    "El planeta debe estar conquistado antes de publicarse en el mercado");

            // Verificar valoración aprobada
            var valoracion = await _context.PlanetaValoraciones
                .FirstOrDefaultAsync(v =>
                    v.Id_Valoracion == dto.Id_Valoracion &&
                    v.Id_Planeta == dto.Id_Planeta &&
                    v.Estado_Valoracion == "Aprobada");

            if (valoracion == null)
                throw new InvalidOperationException(
                    "El planeta debe tener una valoración aprobada para ser publicado en el mercado");

            // Verificar que no exista publicación activa para el mismo planeta
            var publicacionActiva = await _context.MercadoPlanetas
                .AnyAsync(m => m.Id_Planeta == dto.Id_Planeta && m.Activo);

            if (publicacionActiva)
                throw new InvalidOperationException("El planeta ya tiene una publicación activa en el mercado");

            var publicacion = new MercadoPlaneta
            {
                Id_Planeta = dto.Id_Planeta,
                Id_Valoracion = dto.Id_Valoracion,
                Precio_Publicado = dto.Precio_Publicado,
                Precio_Minimo = dto.Precio_Minimo,
                Fecha_Publicacion = DateTime.Now,
                Fecha_Vencimiento = dto.Fecha_Vencimiento,
                Activo = true,
                Id_Publicado_Por = idUsuario,
                Descripcion_Venta = dto.Descripcion_Venta
            };

            _context.MercadoPlanetas.Add(publicacion);
            await _context.SaveChangesAsync();

            await RegistrarAuditoria(
                idUsuario, "PUBLICAR_PLANETA", "mercado_planeta",
                publicacion.Id_Publicacion, null,
                JsonSerializer.Serialize(new { dto.Id_Planeta, dto.Precio_Publicado, dto.Id_Valoracion }),
                ipOrigen);

            return new MercadoListItemDto
            {
                Id_Publicacion = publicacion.Id_Publicacion,
                Id_Planeta = publicacion.Id_Planeta,
                Nombre_Planeta = planeta.Nombre,
                Galaxia = planeta.GalaxiaNav?.Nombre ?? "Desconocida",
                Precio_Publicado = publicacion.Precio_Publicado,
                Clase_Planeta = valoracion.Clase_Planeta,
                Descripcion_Venta = publicacion.Descripcion_Venta,
                Fecha_Vencimiento = publicacion.Fecha_Vencimiento,
                Fecha_Publicacion = publicacion.Fecha_Publicacion
            };
        }

        public async Task<MercadoListItemDto> EditarPublicacion(int id, EditarPublicacionDto dto, int idUsuario, string ipOrigen)
        {
            var publicacion = await _context.MercadoPlanetas
                .Include(m => m.Planeta)
                    .ThenInclude(p => p!.GalaxiaNav)
                .Include(m => m.Valoracion)
                .FirstOrDefaultAsync(m => m.Id_Publicacion == id && m.Activo);

            if (publicacion == null)
                throw new KeyNotFoundException("Publicación no encontrada o ya no está activa");

            var valorAnterior = JsonSerializer.Serialize(new
            {
                publicacion.Activo,
                publicacion.Precio_Publicado,
                publicacion.Descripcion_Venta,
                publicacion.Fecha_Vencimiento
            });

            if (dto.Activo.HasValue)
                publicacion.Activo = dto.Activo.Value;

            if (dto.Precio_Publicado.HasValue)
                publicacion.Precio_Publicado = dto.Precio_Publicado.Value;

            if (dto.Descripcion_Venta != null)
                publicacion.Descripcion_Venta = dto.Descripcion_Venta;

            if (dto.Fecha_Vencimiento.HasValue)
                publicacion.Fecha_Vencimiento = dto.Fecha_Vencimiento.Value;

            _context.MercadoPlanetas.Update(publicacion);
            await _context.SaveChangesAsync();

            await RegistrarAuditoria(
                idUsuario, "EDITAR_PUBLICACION", "mercado_planeta",
                publicacion.Id_Publicacion, valorAnterior,
                JsonSerializer.Serialize(new
                {
                    publicacion.Precio_Publicado,
                    publicacion.Descripcion_Venta,
                    publicacion.Fecha_Vencimiento
                }),
                ipOrigen);

            return new MercadoListItemDto
            {
                Id_Publicacion = publicacion.Id_Publicacion,
                Id_Planeta = publicacion.Id_Planeta,
                Nombre_Planeta = publicacion.Planeta?.Nombre ?? "Desconocido",
                Galaxia = publicacion.Planeta?.GalaxiaNav?.Nombre ?? "Desconocida",
                Precio_Publicado = publicacion.Precio_Publicado,
                Clase_Planeta = publicacion.Valoracion?.Clase_Planeta ?? "D",
                Descripcion_Venta = publicacion.Descripcion_Venta,
                Fecha_Vencimiento = publicacion.Fecha_Vencimiento,
                Fecha_Publicacion = publicacion.Fecha_Publicacion
            };
        }

        public async Task RetirarPlaneta(int id, RetirarMercadoDto dto, int idUsuario, string ipOrigen)
        {
            var publicacion = await _context.MercadoPlanetas
                .FirstOrDefaultAsync(m => m.Id_Publicacion == id && m.Activo);

            if (publicacion == null)
                throw new KeyNotFoundException("Publicación no encontrada o ya está inactiva");

            publicacion.Activo = false;
            _context.MercadoPlanetas.Update(publicacion);
            await _context.SaveChangesAsync();

            await RegistrarAuditoria(
                idUsuario,
                $"RETIRAR_PLANETA. Motivo: {dto.Motivo}",
                "mercado_planeta",
                publicacion.Id_Publicacion,
                "activo: true", "activo: false",
                ipOrigen);
        }

        public async Task EliminarPublicacion(int id, int idUsuario, string ipOrigen)
        {
            var publicacion = await _context.MercadoPlanetas
                .Include(m => m.Planeta)
                .FirstOrDefaultAsync(m => m.Id_Publicacion == id && m.Activo);

            if (publicacion == null)
                throw new KeyNotFoundException("Publicación no encontrada o ya está inactiva");

            publicacion.Activo = false;
            _context.MercadoPlanetas.Update(publicacion);

            if (publicacion.Planeta != null)
            {
                publicacion.Planeta.Id_Estado = 1;
                _context.Planetas.Update(publicacion.Planeta);
            }

            await _context.SaveChangesAsync();

            await RegistrarAuditoria(
                idUsuario, "ELIMINAR_PUBLICACION", "mercado_planeta",
                publicacion.Id_Publicacion, "activo: true", "activo: false",
                ipOrigen);
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
