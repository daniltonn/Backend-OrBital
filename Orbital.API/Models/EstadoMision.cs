using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orbital.API.Models
{
    [Table("estado_mision")]
    public class EstadoMision
    {
        [Key]
        [Column("id_estado_mision")]
        public int Id_Estado_Mision { get; set; }

        [Column("nombre_estado")]
        public string Nombre_Estado { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }
    }
}
