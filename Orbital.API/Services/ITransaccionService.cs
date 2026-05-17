using Orbital.API.DTOs;

namespace Orbital.API.Services
{
    public interface ITransaccionService
    {
        Task<TransaccionListItemDto> ComprarPlaneta(int idPublicacion, int idCliente, ComprarPlanetaDto dto, string ipOrigen);
        Task<TransaccionListItemDto> CambiarEstadoTransaccion(int idTransaccion, CambiarEstadoTransaccionDto dto, int idUsuario, string ipOrigen);
        Task<List<TransaccionListItemDto>> ListarTransacciones(
            string? estado, DateTime? fechaInicio, DateTime? fechaFin, int? idComprador, int? idPublicacion = null);
        Task<List<TransaccionClienteDto>> ListarComprasCliente(int idCliente);
    }
}
