using System;
using System.Net.Http;
using System.Net.Http.Headers;  // Solución del error 8
using System.Threading.Tasks;

namespace AppInsegura.Servicios
{
    public class RedService
    {
        // ----------------------------------------------------------------------
        // CORRECCIÓN 8.º ERROR – DEFENSA EN PROFUNDIDAD (2)
        // Aquí se protege la clave del servidor (API key) con VARIAS capas.
        // ANTES:   la clave estaba escrita en el código, viajaba por HTTP (sin
        //          cifrar) dentro de la URL y además se imprimía por pantalla.
        // PELIGRO: se podía robar mirando el código, espiando la red, o en los
        //          registros e historiales donde se guardan las URLs.
        //
        // CAPA 1: la clave ya no está en el código; se lee de la variable de
        //         entorno GAME_API_KEY (un valor guardado fuera del programa).
        // ----------------------------------------------------------------------
        private static readonly string? ApiKey = Environment.GetEnvironmentVariable("GAME_API_KEY");  // Solución del error 8

        // CAPA 2: solo HTTPS (conexión cifrada). Antes era http://, que viaja en
        //         texto claro y cualquiera en la red podía leer la clave.
        private const string UrlServidor = "https://api.miapp-insegura.local/puntuaciones";  // Solución del error 8

        public void EnviarPuntuacion(string nombreUsuario, int puntuacion)
        {
            // Si no hay clave configurada, no se envía nada.
            if (string.IsNullOrEmpty(ApiKey))  // Solución del error 8
            {
                Console.WriteLine("Falta configurar la variable de entorno GAME_API_KEY.");  // Solución del error 8
                return;  // Solución del error 8
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
            // La URL ya NO lleva la clave: solo el usuario y los puntos.
            string url = $"{UrlServidor}?usuario={nombreUsuario}&puntos={puntuacion}";  // Solución del error 8
            // CAPA 2 (refuerzo): si por error la URL no empieza por https://,
            //         se bloquea y no se envía nada.
            if (!url.StartsWith("https://"))  // Solución del error 8
            {
                throw new InvalidOperationException("Solo se permiten conexiones HTTPS.");  // Solución del error 8
            }

            using var cliente = new HttpClient();
            // CAPA 3: la clave va en la cabecera Authorization ("Bearer <clave>"),
            //         no en la URL. Las URLs se guardan en historiales y logs;
            //         las cabeceras no.
            // CAPA 4: la clave nunca se imprime por pantalla (antes se mostraba
            //         la URL entera, con la clave dentro).
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);  // Solución del error 8

            HttpResponseMessage respuesta = await cliente.GetAsync(url);
            Console.WriteLine($"Respuesta del servidor: {(int)respuesta.StatusCode}");
        }
    }
}
