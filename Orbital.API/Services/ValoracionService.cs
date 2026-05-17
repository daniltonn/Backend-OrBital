using Orbital.API.Data;
using Orbital.API.DTOs;
using Orbital.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Orbital.API.Services
{
    public class ValoracionService : IValoracionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ValoracionService> _logger;
        private readonly CalculadorValorEstrategico _calculador;

        public ValoracionService(
            AppDbContext context,
            ILogger<ValoracionService> logger)
        {
            _context = context;
            _logger = logger;
            _calculador = new CalculadorValorEstrategico();
        }

        /// <summary>
        /// Calcula y almacena la valoración estratégica de un planeta
        /// RF-3.4.2.4 y RF-3.4.2.5
        /// </summary>
        public async Task<ValoracionPlanetaResponseDto> CalcularValorEstrategico(
            int planetaId,
            int analistaId,
            string? observaciones = null,
            decimal? recursosScore = null,
            decimal? tecnologiaScore = null,
            decimal? ubicacionScore = null,
            decimal? poderScore = null,
            decimal? riesgoScore = null)
        {
            _logger.LogInformation($"Iniciando cálculo de valor estratégico para planeta {planetaId}");

            // 1. Validar que planeta existe y está activo
            var planeta = await _context.Planetas
                .Include(p => p.Estado)
                .FirstOrDefaultAsync(p => p.Id_Planeta == planetaId && p.Activo);

            if (planeta == null)
            {
                _logger.LogWarning($"Planeta {planetaId} no encontrado o inactivo");
                throw new ArgumentException($"El planeta con ID {planetaId} no existe o está inactivo");
            }

            // 2. Validar que analista existe
            var analista = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id_Usuario == analistaId && u.Activo);

            if (analista == null)
            {
                _logger.LogWarning($"Analista {analistaId} no encontrado o inactivo");
                throw new ArgumentException($"El analista con ID {analistaId} no existe o está inactivo");
            }

            // 3. Obtener recursos del planeta (solo si no se provee recursosScore manualmente)
            var recursos = await _context.RecursosPlaneta
                .Include(r => r.Recurso)
                .AsNoTracking()
                .Where(r => r.Id_Planeta == planetaId)
                .ToListAsync();

            _logger.LogInformation($"Planeta {planetaId} tiene {recursos.Count} recursos");
            
            foreach (var r in recursos)
{
    decimal baseVal =
        Math.Max(r.Cantidad_Estimada, 0) *
        Math.Max(r.Valor_Unitario, 0);




    _logger.LogInformation(
        $"[RECURSO TRACE] " +
        $"ID:{r.Id_Recurso} | " +
        $"Nombre:{r.Recurso?.Nombre} | " +
        $"Cant:{r.Cantidad_Estimada} | " +
        $"Unit:{r.Valor_Unitario} | " +
        $"Rareza:{r.Recurso?.Rareza} | " +
        $"Extraible:{r.Extraible} | " +
        $"Base:{baseVal} | "
    );
}

            var poblacion = planeta.Poblacion ?? 0;

            _logger.LogInformation(
                $"Datos planeta -> " +
                    $"Poblacion: {planeta.Poblacion}, " +
                    $"Tecnologia: {planeta.Nivel_Tecnologico}, " +
                    $"NivelVida: {planeta.Nivel_Vida_Planeta}, " +
                    $"Galaxia: {planeta.Id_Galaxia}"
            );  

            // 4. Calcular scores: usar los provistos o auto-calcular
            var recursosScoreVal = recursosScore ?? _calculador.CalcularRecursosScore(recursos);
            var poderScoreVal = poderScore ?? _calculador.CalcularPoderScore(planeta.Nivel_Vida_Planeta);
            var dificultadScore = _calculador.CalcularDificultadScore(poderScoreVal);
            var tecnologiaScoreVal = tecnologiaScore ?? _calculador.CalcularTecnologiaScore((int)planeta.Nivel_Tecnologico);
            var ubicacionScoreVal = ubicacionScore ?? _calculador.CalcularUbicacionScore();
            var riesgoScoreVal = riesgoScore ?? _calculador.CalcularRiesgoScore(false);

            _logger.LogInformation($"Scores calculados - Recursos: {recursosScoreVal}, Poder: {poderScoreVal}, Tecnologia: {tecnologiaScoreVal}");

            // 5. Calcular valor total
            var valorTotal = _calculador.CalcularValorTotal(
                recursosScoreVal, poderScoreVal, tecnologiaScoreVal, ubicacionScoreVal, riesgoScoreVal);

            // 6. Clasificar planeta
            var clasePlaneta = _calculador.CalcularClasePlaneta(valorTotal);

            // 7. Calcular precio final
            var precioFinal = _calculador.CalcularPrecioFinal(valorTotal, poblacion, (int)planeta.Nivel_Tecnologico);

            _logger.LogInformation($"Valoración calculada - Total: {valorTotal}, Clase: {clasePlaneta}, Precio: {precioFinal}");

            // 8. Crear y guardar evaluación
            var nuevaValoracion = new PlanetaValoracion
            {
                Id_Planeta = planetaId,
                Recursos_Score = recursosScoreVal,
                Tecnologia_Score = tecnologiaScoreVal,
                Ubicacion_Score = ubicacionScoreVal,
                Poder_Score = poderScoreVal,
                Riesgo_Score = riesgoScoreVal,
                Valor_Total = valorTotal,
                Clase_Planeta = clasePlaneta,
                Precio_Final = precioFinal,
                Id_Analista = analistaId,
                Fecha_Valoracion = DateTime.Now,
                Estado_Valoracion = "Pendiente",
                Observaciones = observaciones
            };

            _context.PlanetaValoraciones.Add(nuevaValoracion);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Valoración guardada con ID {nuevaValoracion.Id_Valoracion}");

            // 9. Retornar DTO de respuesta
            return MapearAResponseDto(nuevaValoracion, planeta, analista, null);
        }

        /// <summary>
        /// Obtiene todas las valoraciones de un planeta específico
        /// RF-3.4.2.6 y RF-3.4.2.8
        /// </summary>
        public async Task<List<ValoracionPlanetaResponseDto>> ObtenerValoracionesPlaneta(int planetaId)
        {
            var valoraciones = await _context.PlanetaValoraciones
                .Where(v => v.Id_Planeta == planetaId)
                .Include(v => v.Planeta)
                .Include(v => v.Analista)
                .Include(v => v.AprobadoPor)
                .OrderByDescending(v => v.Fecha_Valoracion)
                .ToListAsync();

            return valoraciones.Select(v => MapearAResponseDto(v, v.Planeta, v.Analista, v.AprobadoPor)).ToList();
        }

        /// <summary>
        /// Obtiene una valoración específica por su ID
        /// </summary>
        public async Task<ValoracionPlanetaResponseDto?> ObtenerValoracionPorId(int valoracionId)
        {
            var valoracion = await _context.PlanetaValoraciones
                .Include(v => v.Planeta)
                .Include(v => v.Analista)
                .Include(v => v.AprobadoPor)
                .FirstOrDefaultAsync(v => v.Id_Valoracion == valoracionId);

            if (valoracion == null)
                return null;

            return MapearAResponseDto(valoracion, valoracion.Planeta, valoracion.Analista, valoracion.AprobadoPor);
        }

        /// <summary>
        /// Obtiene los factores desglosados de una valoración
        /// RF-3.4.2.7
        /// </summary>
        public async Task<FactoresValoracionDto?> ObtenerFactoresValoracion(int valoracionId)
        {
            var valoracion = await _context.PlanetaValoraciones
                .Include(v => v.Planeta)
                .FirstOrDefaultAsync(v => v.Id_Valoracion == valoracionId);

            if (valoracion == null)
                return null;

            return new FactoresValoracionDto
            {
                Id_Valoracion = valoracion.Id_Valoracion,
                Id_Planeta = valoracion.Id_Planeta,
                Nombre_Planeta = valoracion.Planeta?.Nombre ?? "Desconocido",
                Recursos = new ScoreDetalleDto
                {
                    Nombre = "Recursos",
                    Valor = valoracion.Recursos_Score,
                    Descripcion = $"Valor de recursos extraíbles disponibles (score: {valoracion.Recursos_Score}/10)"
                },
                Tecnologia = new ScoreDetalleDto
                {
                    Nombre = "Tecnología",
                    Valor = valoracion.Tecnologia_Score,
                    Descripcion = $"Nivel tecnológico del planeta (score: {valoracion.Tecnologia_Score}/10)"
                },
                Ubicacion = new ScoreDetalleDto
                {
                    Nombre = "Ubicación",
                    Valor = valoracion.Ubicacion_Score,
                    Descripcion = $"Importancia estratégica de la ubicación (score: {valoracion.Ubicacion_Score}/10)"
                },
                Poder = new ScoreDetalleDto
                {
                    Nombre = "Poder Nativo",
                    Valor = valoracion.Poder_Score,
                    Descripcion = $"Nivel de poder de civilizaciones nativas (score: {valoracion.Poder_Score}/10)"
                },
                Riesgo = new ScoreDetalleDto
                {
                    Nombre = "Riesgo",
                    Valor = valoracion.Riesgo_Score,
                    Descripcion = $"Amenazas y riesgos detectados (score: {valoracion.Riesgo_Score}/10)"
                },
                Valor_Total = valoracion.Valor_Total,
                Clase_Planeta = valoracion.Clase_Planeta,
                Precio_Final = valoracion.Precio_Final,
                Estado_Valoracion = valoracion.Estado_Valoracion
            };
        }

        /// <summary>
        /// Obtiene todas las valoraciones con filtros opcionales
        /// </summary>
        public async Task<List<ValoracionPlanetaResponseDto>> ObtenerTodos(
            string? estado = null,
            int? planetaId = null,
            int? analistaId = null)
        {
            var query = _context.PlanetaValoraciones
                .Include(v => v.Planeta)
                .Include(v => v.Analista)
                .Include(v => v.AprobadoPor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(v => v.Estado_Valoracion == estado);

            if (planetaId.HasValue)
                query = query.Where(v => v.Id_Planeta == planetaId.Value);

            if (analistaId.HasValue)
                query = query.Where(v => v.Id_Analista == analistaId.Value);

            var valoraciones = await query
                .OrderByDescending(v => v.Fecha_Valoracion)
                .ToListAsync();

            return valoraciones.Select(v => MapearAResponseDto(v, v.Planeta, v.Analista, v.AprobadoPor)).ToList();
        }

        /// <summary>
        /// Aprueba una valoración pendiente
        /// </summary>
        public async Task<ValoracionPlanetaResponseDto?> AprobarValoracion(int valoracionId, int aprobadorId)
        {
            var valoracion = await _context.PlanetaValoraciones
                .Include(v => v.Planeta)
                .Include(v => v.Analista)
                .FirstOrDefaultAsync(v => v.Id_Valoracion == valoracionId);

            if (valoracion == null)
            {
                _logger.LogWarning($"Valoración {valoracionId} no encontrada");
                return null;
            }

            if (valoracion.Estado_Valoracion != "Pendiente")
            {
                _logger.LogWarning($"Valoración {valoracionId} no está en estado Pendiente");
                throw new InvalidOperationException($"Solo se pueden aprobar valoraciones en estado Pendiente");
            }

            // Validar aprobador
            var aprobador = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id_Usuario == aprobadorId && u.Activo);

            if (aprobador == null)
            {
                _logger.LogWarning($"Aprobador {aprobadorId} no encontrado");
                throw new ArgumentException($"El aprobador con ID {aprobadorId} no existe");
            }

            valoracion.Estado_Valoracion = "Aprobada";
            valoracion.Aprobado_Por = aprobadorId;
            valoracion.Fecha_Aprobacion = DateTime.Now;

            _context.PlanetaValoraciones.Update(valoracion);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Valoración {valoracionId} aprobada por usuario {aprobadorId}");

            return MapearAResponseDto(valoracion, valoracion.Planeta, valoracion.Analista, aprobador);
        }

        /// <summary>
        /// Rechaza una valoración pendiente
        /// </summary>
        public async Task<bool> RechazarValoracion(int valoracionId, string motivo)
        {
            var valoracion = await _context.PlanetaValoraciones
                .FirstOrDefaultAsync(v => v.Id_Valoracion == valoracionId);

            if (valoracion == null)
            {
                _logger.LogWarning($"Valoración {valoracionId} no encontrada");
                return false;
            }

            if (valoracion.Estado_Valoracion != "Pendiente")
            {
                _logger.LogWarning($"Valoración {valoracionId} no está en estado Pendiente");
                throw new InvalidOperationException($"Solo se pueden rechazar valoraciones en estado Pendiente");
            }

            valoracion.Estado_Valoracion = "Rechazada";
            valoracion.Observaciones = $"Rechazada: {motivo}";

            _context.PlanetaValoraciones.Update(valoracion);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Valoración {valoracionId} rechazada. Motivo: {motivo}");

            return true;
        }

        // =========================
        // HELPERS
        // =========================

        private ValoracionPlanetaResponseDto MapearAResponseDto(
            PlanetaValoracion valoracion,
            Planeta? planeta,
            Usuario? analista,
            Usuario? aprobador)
        {
            return new ValoracionPlanetaResponseDto
            {
                Id_Valoracion = valoracion.Id_Valoracion,
                Id_Planeta = valoracion.Id_Planeta,
                Nombre_Planeta = planeta?.Nombre ?? "Desconocido",
                Recursos_Score = valoracion.Recursos_Score,
                Tecnologia_Score = valoracion.Tecnologia_Score,
                Ubicacion_Score = valoracion.Ubicacion_Score,
                Poder_Score = valoracion.Poder_Score,
                Riesgo_Score = valoracion.Riesgo_Score,
                Valor_Total = valoracion.Valor_Total,
                Clase_Planeta = valoracion.Clase_Planeta,
                Precio_Final = valoracion.Precio_Final,
                Id_Analista = valoracion.Id_Analista,
                Nombre_Analista = analista?.Nombre ?? "Desconocido",
                Fecha_Valoracion = valoracion.Fecha_Valoracion,
                Aprobado_Por = valoracion.Aprobado_Por,
                Nombre_Aprobador = aprobador?.Nombre,
                Fecha_Aprobacion = valoracion.Fecha_Aprobacion,
                Estado_Valoracion = valoracion.Estado_Valoracion,
                Observaciones = valoracion.Observaciones
            };
        }
    }
}
