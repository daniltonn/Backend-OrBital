using Microsoft.AspNetCore.Mvc;
using Orbital.API.Services;

namespace Orbital.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GalaxiasController : ControllerBase
    {
        private readonly GalaxiaService _service;

        public GalaxiasController(GalaxiaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var galaxias = await _service.ObtenerTodas();
            return Ok(galaxias);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var galaxia = await _service.ObtenerPorId(id);

            if (galaxia == null)
                return NotFound();

            return Ok(galaxia);
        }
    }
}