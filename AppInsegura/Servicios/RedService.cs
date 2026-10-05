using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AppInsegura.Servicios
{
    public class RedService
    {
        // CORRECCIÓN (Defensa en profundidad 2): la clave ya no está escrita en
        // el código, se lee de la variable de entorno GAME_API_KEY.
        private static readonly string? ApiKey = Environment.GetEnvironmentVariable("GAME_API_KEY");

        // CORRECCIÓN (Defensa en profundidad 2): solo se permiten conexiones HTTPS.
        private const string UrlServidor = "https://api.miapp-insegura.local/puntuaciones";

        public void EnviarPuntuacion(string nombreUsuario, int puntuacion)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                Console.WriteLine("Falta configurar la variable de entorno GAME_API_KEY.");
                return;
            }

            try
            {
                EnviarPuntuacionAsync(nombreUsuario, puntuacion).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("No se ha podido conectar con el servidor (es normal si no tienes conexión real):");
                Console.WriteLine(ex.Message);
            }
        }

        private async Task EnviarPuntuacionAsync(string nombreUsuario, int puntuacion)
        {
            string url = $"{UrlServidor}?usuario={nombreUsuario}&puntos={puntuacion}";
            if (!url.StartsWith("https://"))
            {
                throw new InvalidOperationException("Solo se permiten conexiones HTTPS.");
            }

            using var cliente = new HttpClient();
            // CORRECCIÓN (Defensa en profundidad 2): la clave viaja en la
            // cabecera Authorization, no en la URL, y no se imprime por pantalla.
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            HttpResponseMessage respuesta = await cliente.GetAsync(url);
            Console.WriteLine($"Respuesta del servidor: {(int)respuesta.StatusCode}");
        }
    }
}
