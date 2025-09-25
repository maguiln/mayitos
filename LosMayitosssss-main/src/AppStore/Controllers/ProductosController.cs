using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using AppStore.Data;
using AppStore.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout.Properties;
using iText.IO.Image;
using iText.Kernel.Colors;

namespace AppStore.Controllers
{
    public class ProductosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const int ProductosPorPagina = 10;

        public ProductosController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Acción para listar productos
        public async Task<IActionResult> Index(int pagina = 1)
        {
            var totalProductos = await _context.Productos.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalProductos / (double)ProductosPorPagina);

            var productos = await _context.Productos
                .Skip((pagina - 1) * ProductosPorPagina)
                .Take(ProductosPorPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;

            return View(productos);
        }

        // Agregar ProductoO
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromForm]Producto producto, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Por favor, complete todos los campos requeridos." });
            }

            try
            {
                // Si hay una imagen, la guardamos
                if (file != null && file.Length > 0)
                {
                    // Validar el formato de la imagen
                    if (!ValidarImagen(file))
                    {
                        return Json(new { success = false, message = "Formato de imagen no válido. Use JPG, PNG o GIF" });
                    }

                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    producto.Imagen = "/images/" + uniqueFileName;
                }
                else
                {
                    producto.Imagen = "/images/noDisponible.jpg";
                }

                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerListaProductos()
        {
            var productos = await _context.Productos.ToListAsync();
            return PartialView("_ListaProductos", productos);
        }

        private bool ValidarImagen(IFormFile imagen)
        {
            // Solo validar formato
            var formatosPermitidos = new[] { "image/jpeg", "image/png", "image/gif" };
            return formatosPermitidos.Contains(imagen.ContentType);
        }

        private async Task<string> GuardarImagen(IFormFile imagen)
        {
            var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);
            var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");
            
            if (!Directory.Exists(rutaCarpeta))
                Directory.CreateDirectory(rutaCarpeta);
            
            var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);
            
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }
            
            return "/imagenes/" + nombreArchivo;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return Json(new { success = false, message = "Producto no encontrado" });
            }
            return Json(new { success = true, producto = producto });
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, [Bind("Nombre,UnidadMedida,ContenidoNeto,Precio,Stock")] Producto producto, IFormFile? file)
        {
            try
            {
                var productoExistente = await _context.Productos.FindAsync(id);
                if (productoExistente == null)
                {
                    return Json(new { success = false, message = "Producto no encontrado" });
                }

                // Actualizar propiedades
                productoExistente.Nombre = producto.Nombre;
                productoExistente.UnidadMedida = producto.UnidadMedida;
                productoExistente.ContenidoNeto = producto.ContenidoNeto;
                productoExistente.Precio = producto.Precio;
                productoExistente.Stock = producto.Stock;

                if (file != null)
                {
                    // Eliminar imagen anterior si existe y no es la default
                    if (!string.IsNullOrEmpty(productoExistente.Imagen) && 
                        !productoExistente.Imagen.Contains("noDisponible.jpg"))
                    {
                        var imagenAnterior = Path.Combine(_webHostEnvironment.WebRootPath, 
                            productoExistente.Imagen.TrimStart('/'));
                        if (System.IO.File.Exists(imagenAnterior))
                        {
                            System.IO.File.Delete(imagenAnterior);
                        }
                    }

                    // Guardar nueva imagen
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    productoExistente.Imagen = "/images/" + uniqueFileName;
                }

                _context.Update(productoExistente);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                {
                    return Json(new { success = false, message = "Producto no encontrado" });
                }

                // PA VALIDAR LAS VENTAS
                // var tieneVentas = await _context.Ventas.AnyAsync(v => v.ProductoId == id);
                // if (tieneVentas) {
                //     return Json(new { success = false, message = "No se puede eliminar el producto porque tiene ventas asociadas" });
                // }

                // Eliminar imagen si existe y no es la default
                if (!string.IsNullOrEmpty(producto.Imagen) && 
                    !producto.Imagen.Contains("noDisponible.jpg"))
                {
                    var rutaImagen = Path.Combine(_webHostEnvironment.WebRootPath, 
                        producto.Imagen.TrimStart('/'));
                    if (System.IO.File.Exists(rutaImagen))
                    {
                        System.IO.File.Delete(rutaImagen);
                    }
                }

                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar el producto: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerSiguienteId()
        {
            try
            {
                int siguienteId = 1;
                if (await _context.Productos.AnyAsync())
                {
                    siguienteId = await _context.Productos.MaxAsync(p => p.IDProducto) + 1;
                }
                return Json(new { success = true, siguienteId = siguienteId });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al obtener el siguiente ID" });
            }
        }

        // Continúa con Editar, Eliminar, Detalles...

        [HttpPost]
        public async Task<IActionResult> GenerarReporte([FromBody] ReporteRequest request)
        {
            try
            {
                var productos = await _context.Productos.ToListAsync();
                
                if (!productos.Any())
                {
                    return Json(new { 
                        success = false, 
                        message = "No hay productos registrados en el inventario" 
                    });
                }

                var reporte = new ReporteInventarioDTO
                {
                    FechaGeneracion = DateTime.Now,
                    FechaInicial = request.FechaInicial,
                    FechaFinal = request.FechaFinal,
                    Productos = productos,
                    ValorTotalInventario = productos.Sum(p => p.Precio * p.Stock),
                    TotalProductos = productos.Count,
                    ProductosStockBajo = productos.Where(p => p.Stock < 15).ToList(),
                    ProductoMayorStock = productos.OrderByDescending(p => p.Stock).First(),
                    ProductoMenorStock = productos.OrderBy(p => p.Stock).First(),
                    PromedioStock = productos.Average(p => p.Stock)
                };

                var pdf = GenerarPDFMejorado(reporte);
                
                return File(pdf, "application/pdf", $"Reporte_Inventario_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Error al generar el reporte: " + ex.Message 
                });
            }
        }

        private byte[] GenerarPDFMejorado(ReporteInventarioDTO reporte)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            try
            {
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                // Logo y encabezado
                var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Logo-hdrLTKDG--transformed.jpeg");
                if (System.IO.File.Exists(logoPath))
                {
                    var logo = new Image(ImageDataFactory.Create(logoPath))
                        .SetWidth(150)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                    document.Add(logo);
                }

                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("India Brava - Reporte de Inventario")
                    .SetFont(boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER));

                // Información del reporte
                document.Add(new Paragraph($"Fecha de generación: {reporte.FechaGeneracion:dd/MM/yyyy HH:mm}")
                    .SetFont(font)
                    .SetFontSize(12));

                document.Add(new Paragraph("Nota: Este reporte muestra el estado actual del inventario")
                    .SetFont(italicFont)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph("\n"));

                // Tabla de productos
                var table = new Table(7).UseAllAvailableWidth();
                string[] headers = { "ID", "U. Medida", "Cont. Neto", "Nombre", "Stock", "Precio Unit.", "Valor Total" };
                
                foreach (var header in headers)
                {
                    table.AddHeaderCell(new Cell()
                        .SetBackgroundColor(new DeviceRgb(242, 242, 242))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .Add(new Paragraph(header).SetFont(boldFont)));
                }

                foreach (var producto in reporte.Productos)
                {
                    var valorTotal = producto.Stock * producto.Precio;
                    table.AddCell(new Cell().SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(producto.IDProducto.ToString())));
                    table.AddCell(new Cell().SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(producto.UnidadMedida)));
                    table.AddCell(new Cell().SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(producto.ContenidoNeto.ToString())));
                    table.AddCell(new Cell().SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(producto.Nombre)));
                    table.AddCell(new Cell().SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(producto.Stock == 1 ? "1 unidad" : $"{producto.Stock} unidades")));
                    table.AddCell(new Cell().SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(producto.Precio.ToString("C"))));
                    table.AddCell(new Cell().SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(valorTotal.ToString("C"))));
                }

                document.Add(table);

                // Resumen y estadísticas
                document.Add(new Paragraph("\nResumen del Inventario")
                    .SetFont(boldFont)
                    .SetFontSize(14));

                document.Add(new Paragraph($"Total de productos: {reporte.TotalProductos}")
                    .SetFont(font)
                    .SetFontSize(12));

                document.Add(new Paragraph($"Valor total del inventario: {reporte.ValorTotalInventario:C}")
                    .SetFont(font)
                    .SetFontSize(12));

                // Productos con stock crítico
                if (reporte.ProductosStockBajo.Any())
                {
                    document.Add(new Paragraph("\nProductos con Stock Crítico (menos de 15 unidades)")
                        .SetFont(boldFont)
                        .SetFontSize(12));

                    foreach (var producto in reporte.ProductosStockBajo)
                    {
                        document.Add(new Paragraph($"• {producto.Nombre}: {(producto.Stock == 1 ? "1 unidad" : $"{producto.Stock} unidades")}")
                            .SetFont(font)
                            .SetFontSize(12));
                    }
                }

                // Productos con mayor y menor stock
                document.Add(new Paragraph("\nEstadísticas de Stock")
                    .SetFont(boldFont)
                    .SetFontSize(12));

                document.Add(new Paragraph($"Producto con mayor stock: {reporte.ProductoMayorStock.Nombre} ({(reporte.ProductoMayorStock.Stock == 1 ? "1 unidad" : $"{reporte.ProductoMayorStock.Stock} unidades")})")
                    .SetFont(font)
                    .SetFontSize(12));

                document.Add(new Paragraph($"Producto con menor stock: {reporte.ProductoMenorStock.Nombre} ({(reporte.ProductoMenorStock.Stock == 1 ? "1 unidad" : $"{reporte.ProductoMenorStock.Stock} unidades")})")
                    .SetFont(font)
                    .SetFontSize(12));

                // Pie de página
                document.Add(new Paragraph("\nEste reporte representa el estado del inventario al momento de su generación.")
                    .SetFont(italicFont)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Close();
                return ms.ToArray();
            }
            catch
            {
                document.Close();
                throw;
            }
        }
    }
} 