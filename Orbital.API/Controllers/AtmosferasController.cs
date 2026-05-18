using Microsoft.AspNetCore.Mvc;
using Orbital.API.Services;

namespace Orbital.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AtmosferasController : ControllerBase
    {
        private readonly AtmosferaService _service;

        public AtmosferasController(AtmosferaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var atmosferas = await _service.ObtenerTodas();
            return Ok(atmosferas);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var atmosfera = await _service.ObtenerPorId(id);

            if (atmosfera == null)
                return NotFound();

            return Ok(atmosfera);
        }
    }
}