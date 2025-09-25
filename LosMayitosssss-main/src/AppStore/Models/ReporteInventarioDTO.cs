namespace AppStore.Models
{
    public class ReporteInventarioDTO
    {
        public DateTime FechaGeneracion { get; set; }
        public DateTime FechaInicial { get; set; }
        public DateTime FechaFinal { get; set; }
        public List<Producto> Productos { get; set; } = new List<Producto>();
        public decimal ValorTotalInventario { get; set; }
        public int TotalProductos { get; set; }
        public List<Producto> ProductosStockBajo { get; set; } = new List<Producto>();
        public Producto ProductoMayorStock { get; set; } = new Producto();
        public Producto ProductoMenorStock { get; set; } = new Producto();
        public double PromedioStock { get; set; }
    }
} 