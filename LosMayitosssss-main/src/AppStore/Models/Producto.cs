using System.ComponentModel.DataAnnotations;

namespace AppStore.Models
{
    public class Producto
    {
        [Key]
        public int IDProducto { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres")]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "La unidad de medida es obligatoria")]
        [StringLength(25, ErrorMessage = "La unidad de medida no puede exceder los 25 caracteres")]
        public string? UnidadMedida { get; set; }

        [Required(ErrorMessage = "El contenido neto es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El contenido neto debe ser mayor a 0")]
        public int ContenidoNeto { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; } = 0;

        [StringLength(255)]
        public string? Imagen { get; set; }
    }
} 