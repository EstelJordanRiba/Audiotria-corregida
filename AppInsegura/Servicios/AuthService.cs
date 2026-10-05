using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AppInsegura.Datos;
using AppInsegura.Modelos;

namespace AppInsegura.Servicios
{
    public class AuthService
    {
        private readonly BaseDatosUsuarios baseDatos;

        public AuthService(BaseDatosUsuarios baseDatos)
        {
            this.baseDatos = baseDatos;
        }

        // CORRECCIÓN (Privilegio mínimo): el registro público ya no recibe el
        // rol; solo crea cuentas con rol "jugador".
        public Usuario Registrar(string nombre, string contrasena)
        {
            return RegistrarConRol(nombre, contrasena, "jugador");
        }

        // Solo se usa desde Program para crear las cuentas de prueba (#if DEBUG).
        internal Usuario RegistrarConRol(string nombre, string contrasena, string rol)
        {
            var nuevo = new Usuario
            {
                Nombre = nombre,
                ContrasenaHash = CalcularHash(contrasena, RandomNumberGenerator.GetBytes(16)),
                Rol = rol,
                TokenSesion = ""
            };

            baseDatos.Agregar(nuevo);
            return nuevo;
        }

        public Usuario? IniciarSesion(string nombre, string contrasena)
        {
            Usuario? usuario = baseDatos.BuscarExacto(nombre);
            if (usuario == null)
            {
                return null;
            }

            // CORRECCIÓN (No inventar criptografía): se recalcula el hash con la
            // misma sal del usuario y se compara en tiempo constante.
            byte[] sal = Convert.FromBase64String(usuario.ContrasenaHash.Split(':')[0]);
            string hashIntento = CalcularHash(contrasena, sal);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(usuario.ContrasenaHash),
                    Encoding.UTF8.GetBytes(hashIntento)))
            {
                return null;
            }

            // CORRECCIÓN (No inventar criptografía 2): en el usuario solo se
            // guarda el hash SHA-256 del token, y el token no se muestra en el log.
            string token = GenerarTokenSesion();
            usuario.TokenSesion = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

            Console.WriteLine($"[LOG] Login correcto -> usuario: {usuario.Nombre}");

            GuardarSesionEnDisco(usuario);

            return usuario;
        }

        // CORRECCIÓN (No inventar criptografía): se sustituye MD5 por PBKDF2 con
        // SHA-256, una sal aleatoria de 16 bytes por usuario y 600.000
        // iteraciones. Se guarda como "sal:hash" (en Base64).
        private string CalcularHash(string contrasena, byte[] sal)
        {
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, 600_000, HashAlgorithmName.SHA256, 32);
            return $"{Convert.ToBase64String(sal)}:{Convert.ToBase64String(hash)}";
        }

        // CORRECCIÓN (No inventar criptografía 2): el token se genera con
        // RandomNumberGenerator (32 bytes aleatorios) en lugar de Random.
        private string GenerarTokenSesion()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        // CORRECCIÓN (No inventar criptografía 2): TokenSesion ahora contiene el
        // hash del token, así que el token real ya no se guarda en texto plano.
        private void GuardarSesionEnDisco(Usuario usuario)
        {
            File.WriteAllText("sesion.txt", $"{usuario.Nombre}:{usuario.TokenSesion}");
        }
    }
}
