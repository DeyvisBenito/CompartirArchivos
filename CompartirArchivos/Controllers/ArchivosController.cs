using CompartirArchivos.Models;
using CompartirArchivos.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompartirArchivos.Controllers
{
    public class ArchivosController : Controller
    {
        private readonly IRepositorioArchivos repositorioArchivos;
        private readonly string _rutaUploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ArchivosCargados");
        private readonly string _rutaRecibidos = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ArchivosRecibidos");

        public ArchivosController(IRepositorioArchivos repositorioArchivos)
        {
            this.repositorioArchivos = repositorioArchivos;
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Archivos()
        {
            var archivos = ListarArchivos();
            return View(archivos);
        }
        public IActionResult ArchivosRecibidos()
        {
            var archivos = ListarArchivosRecibidos();
            return View(archivos);
        }

        [HttpPost]
        public async Task<IActionResult> CargarArchivo(IFormFile archivo)
        {
            if (archivo != null && archivo.Length > 0)
            {
                var rutaCompleta = Path.Combine(_rutaUploads, archivo.FileName);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                return RedirectToAction("Archivos");
            }

            return View(); 
        }


        [HttpPost]
        public IActionResult EliminarArchivo(string nombreArchivo)
        {
            if (!string.IsNullOrEmpty(nombreArchivo))
            {
                var rutaCompleta = Path.Combine(_rutaUploads, nombreArchivo);

                if (System.IO.File.Exists(rutaCompleta))
                {
                    System.IO.File.Delete(rutaCompleta);
                    return RedirectToAction("Archivos");
                }
                
            }

            return RedirectToAction("Archivos"); 
        }

        [HttpPost]
        public IActionResult EliminarArchivoRecibido(string nombreArchivo)
        {
            if (!string.IsNullOrEmpty(nombreArchivo))
            {
                var rutaCompleta = Path.Combine(_rutaRecibidos, nombreArchivo);

                if (System.IO.File.Exists(rutaCompleta))
                {
                    System.IO.File.Delete(rutaCompleta);
                    return RedirectToAction("ArchivosRecibidos");
                }

            }

            return RedirectToAction("Archivos");
        }

        [HttpPost]
        public async Task<IActionResult> EnviarArchivo(string nombreArchivo)
        {
            
            string urlArchivo = $"{Request.Scheme}://{Request.Host}/ArchivosCargados/{nombreArchivo}";

            // URL del endpoint de la API en el otro servidor (cambiar dependiendo la direccion del servidor)
            string urlDestino = "http://192.168.0.199/api/recibir-archivo";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    
                    var response = await httpClient.GetAsync(urlArchivo);
                    if (response.IsSuccessStatusCode)
                    {
                        var contenidoArchivo = await response.Content.ReadAsByteArrayAsync();

                        
                        var formData = new MultipartFormDataContent();
                        var contenido = new ByteArrayContent(contenidoArchivo);
                        contenido.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                        formData.Add(contenido, "archivo", nombreArchivo);

                        
                        var result = await httpClient.PostAsync(urlDestino, formData);
                        if (result.IsSuccessStatusCode)
                        {
                            return RedirectToAction("Archivos");
                        }
                    }
                }
                catch (Exception ex)
                {
                    
                }
            }

            
            return View("Archivos");
        }

        //EndPoint de la API a donde caera el envio desde el otro servidor
        [HttpPost("api/recibir-archivo")]
        public async Task<IActionResult> RecibirArchivo(IFormFile archivo)
        {
            if (archivo != null && archivo.Length > 0)
            {
                var rutaDestino = Path.Combine(_rutaRecibidos, archivo.FileName);

                using (var stream = new FileStream(rutaDestino, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                return Ok();
            }
            return BadRequest();
        }

        private List<ArchivosModel> ListarArchivos()
        {
            var archivos = Directory.GetFiles(_rutaUploads)
                .Select(x => new ArchivosModel
                {
                    Nombre = Path.GetFileName(x),
                    Url = Url.Content("~/ArchivosCargados/" + Path.GetFileName(x))
                }).ToList();

            return archivos;
        }

        private List<ArchivosModel> ListarArchivosRecibidos()
        {
            var archivos = Directory.GetFiles(_rutaRecibidos)
                .Select(x => new ArchivosModel
                {
                    Nombre = Path.GetFileName(x),
                    Url = Url.Content("~/ArchivosRecibidos/" + Path.GetFileName(x))
                }).ToList();

            return archivos;
        }
    }
}
