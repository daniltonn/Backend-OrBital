namespace Orbital.API.DTOs
{
    public class TransaccionListItemDto
    {
        public int Id_Transaccion { get; set; }
        public string Nombre_Planeta { get; set; } = null!;
        public string Nombre_Comprador { get; set; } = null!;
        public string? Comprador_Tipo { get; set; }
        public string? Vendedor_Nombre { get; set; }
        public decimal Precio_Final { get; set; }
        public string Metodo_Pago { get; set; } = null!;
        public DateTime Fecha_Transaccion { get; set; }
        public string Estado_Transaccion { get; set; } = null!;
        public string? Notas { get; set; }
    }

    public class TransaccionClienteDto
    {
        public int Id_Transaccion { get; set; }
        public string Nombre_Planeta { get; set; } = null!;
        public decimal Precio_Final { get; set; }
        public DateTime Fecha_Transaccion { get; set; }
        public string Estado_Transaccion { get; set; } = null!;
    }
}
