using Orbital.API.Models;

namespace Orbital.API.Services
{
    public class CalculadorValorEstrategico
    {
        // =========================
        // PESOS DEL SISTEMA
        // =========================

        private const decimal PESO_RECURSOS   = 0.4m;
        private const decimal PESO_TECNOLOGIA = 0.2m;
        private const decimal PESO_UBICACION  = 0.2m;
        private const decimal PESO_PODER      = 0.1m;
        private const decimal PESO_RIESGO     = 0.1m;

        // =========================
        // RECURSOS
        // =========================

        /// <summary>
        /// Score económico de recursos.
        /// Rango: 0 - 10
        /// </summary>
        public decimal CalcularRecursosScore(List<RecursoPlaneta> recursos)
        {
            if (!recursos.Any())
                return 0m;

            decimal valorTotal = 0m;

            foreach (var recurso in recursos)
            {
                decimal valorBase =
                    Math.Max(recurso.Cantidad_Estimada, 0) * 
                    Math.Max(recurso.Valor_Unitario, 0);

                // Penalización si no es extraíble
                if (!recurso.Extraible)
                    valorBase *= 0.35m;


            decimal log = (decimal)Math.Log10((double)valorBase + 1);
            decimal valorNormalizado = Math.Min(log / 8m * 10m, 10m);

                // Bonus por rareza
                decimal multiplicadorRareza =
                ObtenerMultiplicadorRareza(recurso.Recurso?.Rareza);

                valorTotal += valorNormalizado * multiplicadorRareza;
            }

            decimal score = Math.Min(valorTotal / recursos.Count, 10m);

            return Math.Round(score, 2);
        }

        private decimal ObtenerMultiplicadorRareza(string? rareza)
        {
            return rareza?.ToLower() switch
            {
                "común"       => 1.0m,
                "poco común" => 1.3m,
                "raro"       => 1.7m,
                "muy raro"   => 2.2m,
                "único"      => 3.0m,
                _            => 1.0m
            };
        }

        // =========================
        // PODER
        // =========================

        /// <summary>
        /// Dificultad militar de invasión.
        /// Rango: 0 - 10
        /// </summary>
        public decimal CalcularPoderScore(string nivelVidaPlaneta)
        {
            return nivelVidaPlaneta?.ToLower() switch
            {
                "sin vida"  => 0m,
                "primitiva" => 2m,
                "bajo"      => 4m,
                "medio"     => 6m,
                "alto"      => 8m,
                "avanzado"  => 10m,
                _           => 0m
            };
        }

        // =========================
        // TECNOLOGÍA
        // =========================

        /// <summary>
        /// Valor tecnológico del planeta.
        /// Rango: 1 - 10
        /// </summary>
        public decimal CalcularTecnologiaScore(int nivelTecnologico)
        {
            decimal score = nivelTecnologico switch
            {
                1 => 2m,   // Primitivo
                2 => 4.5m,   // Medieval
                3 => 7.5m,   // Avanzado
                4 => 10m,  // Interestelar
                _ => 1m
            };

            return Math.Round(score, 2);
        }

        // =========================
        // UBICACIÓN
        // =========================

        /// <summary>
        /// Valor estratégico galáctico.
        /// Rango: 1 - 10
        /// </summary>
        public decimal CalcularUbicacionScore(int? galaxiaId)
        {
            if (!galaxiaId.HasValue)
                return 2m;

            return galaxiaId.Value switch
            {
                1 => 10m, // Núcleo imperial
                2 => 8m,  // Zona comercial
                3 => 6m,  // Zona colonizada
                4 => 4m,  // Periferia
                _ => 5m
            };
        }

        // =========================
        // RIESGO
        // =========================

        /// <summary>
        /// Riesgo de ocupación prolongada.
        /// Basado en población + tecnología.
        /// Rango: 0 - 10
        /// </summary>
        public decimal CalcularRiesgoScore(
            long poblacion,
            int nivelTecnologico)
        {
            decimal poblacionBase =
                ObtenerPoblacionBase(poblacion);

            decimal multiplicadorTecnologia =
                ObtenerMultiplicadorTecnologia(
                    nivelTecnologico);

            decimal riesgo =
                poblacionBase *
                multiplicadorTecnologia;

            return Math.Round(
                Math.Min(riesgo, 10m),2);
        }

        private decimal ObtenerPoblacionBase(long poblacion)
        {
            poblacion = Math.Max(poblacion, 0);
               if (poblacion == 0)
                 return 0m;

            // Escala logarítmica
            double score =
                Math.Log10(poblacion);

            // Normalización aproximada
            decimal resultado =
                (decimal)(score / 12.0 * 10.0);

            return Math.Round(
                Math.Min(resultado, 10m),
                2
            );
        }

        private decimal ObtenerMultiplicadorTecnologia(
            int nivelTecnologico)
        {
            return nivelTecnologico switch
            {
                1 => 0.4m,
                2 => 0.7m,
                3 => 1.2m,
                4 => 1.8m,
                _ => 1m
            };
        }

        // =========================
        // VALOR TOTAL
        // =========================

        /// <summary>
        /// Valor estratégico final.
        /// Rango: 0 - 100
        /// </summary>
        public decimal CalcularValorTotal(
            decimal recursosScore,
            decimal poderScore,
            decimal tecnologiaScore,
            decimal ubicacionScore,
            decimal riesgoScore)
        {

            // =========================
            // 1. VIABILIDAD (FILTRO BASE)
            // =========================


            // planeta prácticamente inútil
            if (recursosScore < 0.5m && tecnologiaScore < 1.5m)
                return 5m;


            // =========================
            // 2. SCORE BASE (ECONOMÍA REAL)
            // =========================

              decimal recursosNorm = recursosScore / 10m;
              decimal tecnologiaNorm = tecnologiaScore / 10m;
              decimal ubicacionNorm = ubicacionScore / 10m;

              decimal poderNorm = poderScore / 10m;
              decimal riesgoNorm = riesgoScore / 10m;

                // =========================
                // 3. SCORE ECONÓMICO BASE (0–1)
                // =========================

                decimal economico =
                    (recursosNorm * PESO_RECURSOS) +
                    (tecnologiaNorm * PESO_TECNOLOGIA) +
                    (ubicacionNorm * PESO_UBICACION);

                // =========================
                // 4. AMENAZA (0–1)
                // =========================

                decimal amenaza =
                    (poderNorm * PESO_PODER) +
                    (riesgoNorm * PESO_RIESGO);

                // penalización suave y estable 
                decimal factorSeguridad = 1m - (amenaza * 0.6m);

                // evitar negativos
                factorSeguridad = Math.Clamp(factorSeguridad, 0.3m, 1m);

                // =========================
                // 5. UBICACIÓN COMO MULTIPLICADOR CONTROLADO
                // =========================

                decimal factorUbicacion = 0.85m + (ubicacionNorm * 0.3m);
                // rango: 0.85 → 1.15

                // =========================
                // 6. SCORE FINAL NORMALIZADO
                // =========================

                decimal score = economico * factorSeguridad * factorUbicacion;

                // convertir a escala 0–100
                decimal resultado = score * 100m;

                return Math.Round(Math.Clamp(resultado, 0m, 100m),2);

        }

        // =========================
        // CLASE PLANETA
        // =========================

        public string CalcularClasePlaneta(
            decimal valorTotal)
        {
            return valorTotal switch
            {
                >= 80m => "A", // mundos imperiales / joyas del imperio
                >= 65m => "B", // mundos estratégicos / conquista prioritaria
                >= 40m => "C", // mundos útiles / colonización estándar
                _      => "D" // mundos débiles / sacrificables
            };
        }

        // =========================
        // PRECIO FINAL
        // =========================

        /// <summary>
        /// Precio galáctico final.
        /// Escala exponencial para que
        /// los planetas premium sean
        /// extremadamente costosos.
        /// </summary>
        public decimal CalcularPrecioFinal(
            decimal valorTotal,
            long poblacion,
            int nivelTecnologico)
        {
                // =========================
                // BASE ECONÓMICA
                // =========================

                decimal precioBase = 1_000_000m;

                // Escalado principal por valor estratégico
                double factorValor =
                    Math.Pow((double)(valorTotal / 100m), 2.2);

                decimal precio =
                    precioBase * (decimal)factorValor;

                // =========================
                // BONUS POR POBLACIÓN
                // =========================

                decimal bonusPoblacion =
                    poblacion switch
                    {
                        < 1_000_000       => 1.2m,
                        < 100_000_000     => 1.4m,
                        < 1_000_000_000   => 1.6m,
                        _                 => 2.0m
                    };

                // =========================
                // BONUS TECNOLÓGICO
                // =========================

                decimal bonusTecnologia =
                    nivelTecnologico switch
                    {
                        1 => 1.0m,
                        2 => 1.2m,
                        3 => 1.5m,
                        4 => 2.0m,
                        _ => 1.0m
                    };

                // =========================
                // PRECIO FINAL
                // =========================

                precio *= bonusPoblacion;
                precio *= bonusTecnologia;

                return Math.Round(precio, 2);
        }
    }

}

        