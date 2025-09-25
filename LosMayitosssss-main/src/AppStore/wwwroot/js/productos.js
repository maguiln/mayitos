document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('formCrearProducto');
    
    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        
        // Validar el formulario
        if (!validarFormulario()) {
            return;
        }

        // Crear FormData para enviar los datos incluyendo la imagen
        const formData = new FormData(form);

        try {
            const response = await fetch('/Productos/Crear', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                // Actualizar la tabla de productos
                const resultado = await response.json();
                if (resultado.success) {
                    // Actualizar la tabla
                    actualizarTablaProductos();
                    // Cerrar el modal
                    const modal = bootstrap.Modal.getInstance(document.getElementById('modalCrearProducto'));
                    modal.hide();
                    // Mostrar mensaje de éxito
                    mostrarMensaje('Producto creado exitosamente', 'success');
                    // Limpiar el formulario
                    form.reset();
                } else {
                    mostrarMensaje(resultado.message, 'error');
                }
            } else {
                throw new Error('Error al crear el producto');
            }
        } catch (error) {
            mostrarMensaje('Error al procesar la solicitud', 'error');
            console.error(error);
        }
    });
});

function validarFormulario() {
    const form = document.getElementById('formCrearProducto');
    const imagen = form.querySelector('#Imagen');
    
    // Validar tamaño de imagen
    if (imagen.files.length > 0) {
        const file = imagen.files[0];
        if (file.size > 5 * 1024 * 1024) { // 5MB
            mostrarMensaje('La imagen no debe exceder los 5MB', 'error');
            return false;
        }
        
        // Validar formato de imagen
        const formatosPermitidos = ['image/jpeg', 'image/png', 'image/gif'];
        if (!formatosPermitidos.includes(file.type)) {
            mostrarMensaje('Formato de imagen no válido. Use JPG, PNG o GIF', 'error');
            return false;
        }
    }

    return true;
}

async function actualizarTablaProductos() {
    try {
        const response = await fetch('/Productos/ObtenerListaProductos');
        if (response.ok) {
            const html = await response.text();
            document.getElementById('tablaProductos').innerHTML = html;
        }
    } catch (error) {
        console.error('Error al actualizar la tabla:', error);
    }
}

function mostrarMensaje(mensaje, tipo) {
    Swal.fire({
        text: mensaje,
        icon: tipo,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000
    });
}

function guardarProducto() {
    if (!validarFormulario()) {
        return;
    }

    var formData = new FormData($('#formProducto')[0]);
    
    // Asegurarse de que el archivo se está agregando correctamente
    var fileInput = $('#file')[0];
    if (fileInput.files.length > 0) {
        formData.append('file', fileInput.files[0]);
    }

    $.ajax({
        url: '@Url.Action("Crear", "Productos")',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function(response) {
            if (response.success) {
                Swal.fire({
                    title: '¡Éxito!',
                    text: 'Producto guardado correctamente',
                    icon: 'success'
                }).then(() => {
                    $('#modalProducto').modal('hide');
                    location.reload();
                });
            } else {
                Swal.fire('Error', response.message, 'error');
            }
        },
        error: function() {
            Swal.fire('Error', 'Ocurrió un error al guardar el producto', 'error');
        }
    });
} 