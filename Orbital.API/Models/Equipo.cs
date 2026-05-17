using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orbital.API.Models
{
    [Table("equipo")]
    public class Equipo
    {
        [Key]
        [Column("id_equipo")]
        public int Id_Equipo { get; set; }

        [Column("nombre_equipo")]
        public string Nombre_Equipo { get; set; } = null!;

        [Column("tipo_equipo")]
        public string Tipo_Equipo { get; set; } = null!;

        [Column("nivel_poder_promedio")]
        public int Nivel_Poder_Promedio { get; set; }

        [Column("capacidad_maxima")]
        public byte Capacidad_Maxima { get; set; } = 5;

        [Column("disponible")]
        public bool Disponible { get; set; } = true;

        [Column("id_lider")]
        public int? Id_Lider { get; set; }

        [Column("fecha_creacion")]
        public DateTime Fecha_Creacion { get; set; }
    }
}
