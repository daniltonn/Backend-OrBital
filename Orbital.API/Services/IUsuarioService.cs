using Orbital.API.DTOs;

namespace Orbital.API.Services
{
    public interface IUsuarioService
    {
        Task<List<UsuarioResponseDto>> GetUsuarios();
        Task<List<UsuarioResponseDto>> ListarUsuarios(
            string? nombre, bool? activo,
            DateTime? fechaDesde, DateTime? fechaHasta,
            int? jerarquiaId, string? letra,
            int? nivelPoderMin, int? nivelPoderMax,
            string? ordenarPor, bool desc);
        Task<List<UsuarioResponseDto>> ObtenerUltimos3PorRol(int idRol);
        Task<UsuarioResponseDto?> GetUsuarioById(int id);
        Task<UsuarioDetalleDto?> ObtenerDetalleUsuario(int id);
        Task UpdateUsuario(int id, UsuarioUpdateDto dto);
    }
}
