using Orbital.API.DTOs;

namespace Orbital.API.Services
{
    public interface IClienteService
    {
        Task<List<ClienteResponseDto>> Listar(string? tipo, string? nivelConfianza, bool? activo);
        Task<ClienteResponseDto?> ObtenerPorId(int id);
        Task<ClienteDetalleDto?> ObtenerDetalleConCompras(int id);
        Task<ClienteResponseDto> Crear(ClienteCreateAdminDto dto);
        Task<ClienteResponseDto> Actualizar(int id, ClienteUpdateDto dto, int idUsuario, string ipOrigen);
        Task<ClienteResponseDto> AjustarCredito(int id, CreditoAjusteDto dto, int idUsuario, string ipOrigen);
    }
}
