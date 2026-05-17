using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Orbital.API.Data;
using Orbital.API.DTOs;
using Orbital.API.Models;

namespace Orbital.API.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly AppDbContext _context;

        public UsuarioService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UsuarioResponseDto>> GetUsuarios()
        {
            return await _context.Usuarios
                .Include(x => x.Rol)
                .Include(x => x.Jerarquia)
                .Select(x => new UsuarioResponseDto
                {
                    Id_Usuario = x.Id_Usuario,
                    Nombre = x.Nombre,
                    Correo = x.Correo,
                    Rol = x.Rol.Nombre_Rol,
                    Jerarquia = x.Jerarquia.Nombre_Jerarquia,
                    Activo = x.Activo
                })
                .ToListAsync();
        }

        public async Task<UsuarioResponseDto?> GetUsuarioById(int id)
        {
            return await _context.Usuarios
                .Include(x => x.Rol)
                .Include(x => x.Jerarquia)
                .Where(x => x.Id_Usuario == id)
                .Select(x => new UsuarioResponseDto
                {
                    Id_Usuario = x.Id_Usuario,
                    Nombre = x.Nombre,
                    Correo = x.Correo,
                    Rol = x.Rol.Nombre_Rol,
                    Jerarquia = x.Jerarquia.Nombre_Jerarquia,
                    Activo = x.Activo
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UsuarioDetalleDto?> ObtenerDetalleUsuario(int id)
        {
            var datos = await (
                from u in _context.Usuarios
                join r in _context.Roles on u.Id_Rol equals r.Id_Rol
                join j in _context.Jerarquias on u.Id_Jerarquia equals j.Id_Jerarquia
                where u.Id_Usuario == id
                select new
                {
                    u.Id_Usuario,
                    u.Nombre,
                    u.Correo,
                    u.Contrasena_Hash,
                    NombreRol = r.Nombre_Rol,
                    NombreJerarquia = j.Nombre_Jerarquia,
                    u.Activo,
                    u.Fecha_Registro
                }
            ).FirstOrDefaultAsync();

            if (datos == null) return null;

            var membresia = await _context.MiembrosEquipo
                .Where(m => m.Id_Usuario == id && m.Activo)
                .FirstOrDefaultAsync();

            EquipoDetalleDto? equipoDto = null;
            List<MisionDetalleDto> misiones = new();

            if (membresia != null)
            {
                var equipo = await _context.Equipos.FindAsync(membresia.Id_Equipo);

                var miembros = await (
                    from m in _context.MiembrosEquipo
                    join u in _context.Usuarios on m.Id_Usuario equals u.Id_Usuario
                    join r in _context.Roles on u.Id_Rol equals r.Id_Rol
                    where m.Id_Equipo == membresia.Id_Equipo && m.Activo
                    select new MiembroEquipoDetalleDto
                    {
                        Id_Usuario = u.Id_Usuario,
                        Nombre = u.Nombre,
                        Rol = r.Nombre_Rol,
                        Nivel_Poder = m.Nivel_Poder
                    }
                ).ToListAsync();

                if (equipo != null)
                {
                    equipoDto = new EquipoDetalleDto
                    {
                        Id_Equipo = equipo.Id_Equipo,
                        Nombre_Equipo = equipo.Nombre_Equipo,
                        Tipo_Equipo = equipo.Tipo_Equipo,
                        Miembros = miembros
                    };
                }

                misiones = await (
                    from m in _context.Misiones
                    join em in _context.EstadosMision on m.Id_Estado_Mision equals em.Id_Estado_Mision
                    join p in _context.Planetas on m.Id_Planeta equals p.Id_Planeta
                    where m.Id_Equipo == membresia.Id_Equipo
                    orderby m.Fecha_Asignacion descending
                    select new MisionDetalleDto
                    {
                        Id_Mision = m.Id_Mision,
                        Nombre_Mision = m.Nombre_Mision,
                        Tipo_Mision = m.Tipo_Mision,
                        Nombre_Planeta = p.Nombre,
                        Estado = em.Nombre_Estado,
                        Prioridad = m.Prioridad,
                        Porcentaje_Avance = m.Porcentaje_Avance,
                        Fecha_Asignacion = m.Fecha_Asignacion,
                        Fecha_Inicio = m.Fecha_Inicio,
                        Fecha_Fin_Estimada = m.Fecha_Fin_Estimada
                    }
                ).ToListAsync();
            }

            return new UsuarioDetalleDto
            {
                Id_Usuario = datos.Id_Usuario,
                Nombre = datos.Nombre,
                Correo = datos.Correo,
                Contrasena_Hash = datos.Contrasena_Hash,
                Rol = datos.NombreRol,
                Jerarquia = datos.NombreJerarquia,
                Activo = datos.Activo,
                Fecha_Registro = datos.Fecha_Registro,
                Nivel_Poder = membresia?.Nivel_Poder,
                Equipo = equipoDto,
                Misiones = misiones
            };
        }

        public async Task UpdateUsuario(int id, UsuarioUpdateDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return;

            if (dto.Nombre != null)
                usuario.Nombre = dto.Nombre;

            if (dto.Correo != null)
                usuario.Correo = dto.Correo;

            if (dto.Contrasena != null)
                usuario.Contrasena_Hash = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena);

            if (dto.IdRol != null)
                usuario.Id_Rol = dto.IdRol.Value;

            if (dto.Activo != null)
                usuario.Activo = dto.Activo.Value;

            if (dto.IdJerarquia != null)
                usuario.Id_Jerarquia = dto.IdJerarquia.Value;

            // Nivel de poder y equipo viven en miembro_equipo
            if (dto.NivelPoder != null || (dto.IdEquipo != null && dto.IdEquipo != 0))
            {
                var miembro = await _context.MiembrosEquipo
                    .Where(m => m.Id_Usuario == id && m.Activo)
                    .FirstOrDefaultAsync();

                if (dto.IdEquipo != null && dto.IdEquipo != 0 && (miembro == null || miembro.Id_Equipo != dto.IdEquipo.Value))
                {
                    if (miembro != null)
                        miembro.Activo = false;

                    var nuevaMembresia = new MiembroEquipo
                    {
                        Id_Equipo = dto.IdEquipo.Value,
                        Id_Usuario = id,
                        Nivel_Poder = dto.NivelPoder ?? miembro?.Nivel_Poder ?? 0,
                        Rol_Equipo = "Combate",
                        Fecha_Ingreso = DateTime.UtcNow,
                        Activo = true
                    };
                    _context.MiembrosEquipo.Add(nuevaMembresia);
                }
                else if (miembro != null && dto.NivelPoder != null)
                {
                    miembro.Nivel_Poder = dto.NivelPoder.Value;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<UsuarioResponseDto>> ListarUsuarios(
            string? nombre, bool? activo,
            DateTime? fechaDesde, DateTime? fechaHasta,
            int? jerarquiaId, string? letra,
            int? nivelPoderMin, int? nivelPoderMax,
            string? ordenarPor, bool desc)
        {
            var query = from u in _context.Usuarios
                        join r in _context.Roles on u.Id_Rol equals r.Id_Rol
                        join j in _context.Jerarquias on u.Id_Jerarquia equals j.Id_Jerarquia
                        join me in _context.MiembrosEquipo.Where(m => m.Activo)
                            on u.Id_Usuario equals me.Id_Usuario into miembros
                        from miembro in miembros.DefaultIfEmpty()
                        select new
                        {
                            u.Id_Usuario,
                            u.Nombre,
                            u.Correo,
                            NombreRol = r.Nombre_Rol,
                            NombreJerarquia = j.Nombre_Jerarquia,
                            u.Activo,
                            u.Fecha_Registro,
                            u.Id_Jerarquia,
                            NivelPoder = (int?)miembro.Nivel_Poder,
                            j.Nivel_Poder_Minimo
                        };

            if (!string.IsNullOrEmpty(nombre))
                query = query.Where(x => x.Nombre.Contains(nombre));

            if (activo.HasValue)
                query = query.Where(x => x.Activo == activo.Value);

            if (fechaDesde.HasValue)
                query = query.Where(x => x.Fecha_Registro >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(x => x.Fecha_Registro <= fechaHasta.Value);

            if (jerarquiaId.HasValue)
                query = query.Where(x => x.Id_Jerarquia == jerarquiaId.Value);

            if (!string.IsNullOrEmpty(letra))
                query = query.Where(x => x.Nombre.StartsWith(letra));

            if (nivelPoderMin.HasValue)
                query = query.Where(x => x.NivelPoder >= nivelPoderMin.Value);

            if (nivelPoderMax.HasValue)
                query = query.Where(x => x.NivelPoder <= nivelPoderMax.Value);

            var ordenada = (ordenarPor?.ToLower(), desc) switch
            {
                ("fecha", false)       => query.OrderBy(x => x.Fecha_Registro),
                ("fecha", true)        => query.OrderByDescending(x => x.Fecha_Registro),
                ("nivel_poder", false) => query.OrderBy(x => x.NivelPoder),
                ("nivel_poder", true)  => query.OrderByDescending(x => x.NivelPoder),
                ("jerarquia", false)   => query.OrderBy(x => x.Nivel_Poder_Minimo),
                ("jerarquia", true)    => query.OrderByDescending(x => x.Nivel_Poder_Minimo),
                (_, false)             => query.OrderBy(x => x.Nombre),
                (_, true)              => query.OrderByDescending(x => x.Nombre),
            };

            var resultado = await ordenada.ToListAsync();

            return resultado.Select(x => new UsuarioResponseDto
            {
                Id_Usuario = x.Id_Usuario,
                Nombre = x.Nombre,
                Correo = x.Correo,
                Rol = x.NombreRol,
                Jerarquia = x.NombreJerarquia,
                Activo = x.Activo,
                Fecha_Registro = x.Fecha_Registro,
                Nivel_Poder = x.NivelPoder
            }).ToList();
        }

        public async Task<List<UsuarioResponseDto>> ObtenerUltimos3PorRol(int idRol)
        {
            return await (
                from u in _context.Usuarios
                join r in _context.Roles on u.Id_Rol equals r.Id_Rol
                join j in _context.Jerarquias on u.Id_Jerarquia equals j.Id_Jerarquia
                join me in _context.MiembrosEquipo.Where(m => m.Activo)
                    on u.Id_Usuario equals me.Id_Usuario into miembros
                from miembro in miembros.DefaultIfEmpty()
                where u.Id_Rol == idRol
                orderby u.Fecha_Registro descending
                select new UsuarioResponseDto
                {
                    Id_Usuario = u.Id_Usuario,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    Rol = r.Nombre_Rol,
                    Jerarquia = j.Nombre_Jerarquia,
                    Activo = u.Activo,
                    Fecha_Registro = u.Fecha_Registro,
                    Nivel_Poder = miembro != null ? miembro.Nivel_Poder : null
                }
            ).Take(3).ToListAsync();
        }
    }
}