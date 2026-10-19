   using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AppInsegura.Modelos;

namespace AppInsegura.Datos
{
    // Simula una tabla de base de datos (equivalente a una tabla SQLite/sqflite).
    // No usa un motor real para que el proyecto compile sin dependencias externas.
    public class BaseDatosUsuarios
    {
        private readonly List<Usuario> usuarios = new List<Usuario>();

        public void Agregar(Usuario usuario)
        {
            usuarios.Add(usuario);
        }

        public List<Usuario> ListarTodos()
        {
            return usuarios;
        }

        public Usuario? BuscarExacto(string nombre)
        {
            return usuarios.FirstOrDefault(u => u.Nombre == nombre);
        }

        // ----------------------------------------------------------------------
        // CORRECCIÓN 2.º ERROR – NO CONFIAR EN LA ENTRADA (inyección SQL)
        // ANTES:   el nombre que escribía el usuario se pegaba tal cual dentro
        //          del texto de la consulta (simulada), del estilo:
        //              WHERE nombre = '<lo que escribe el usuario>'
        // PELIGRO: si alguien escribía      ' OR '1'='1
        //          la consulta quedaba:     WHERE nombre = '' OR '1'='1'
        //          Como 1=1 SIEMPRE es verdad, devolvía el primer usuario
        //          sin saber su nombre. Esto se llama INYECCIÓN SQL.
        // AHORA:   lo que escribe el usuario se trata como un DATO, nunca como
        //          parte de la orden:
        //   1) LISTA BLANCA: solo se acepta si cumple  ^[A-Za-z0-9_]{3,20}$
        //        ^ y $         → desde el principio hasta el final del texto
        //        [A-Za-z0-9_]  → solo letras, números y guion bajo
        //        {3,20}        → entre 3 y 20 caracteres
        //      La comilla ' no está permitida, así que el truco no funciona.
        //   2) Se busca el nombre EXACTO (BuscarExacto), igual que haría una
        //      CONSULTA PARAMETRIZADA.
        // ----------------------------------------------------------------------
        public Usuario? BuscarPorNombre(string nombreBuscado)
        {
            // Si no cumple la lista blanca → se rechaza y no se busca nada.
            if (!Regex.IsMatch(nombreBuscado, "^[A-Za-z0-9_]{3,20}$"))
            {
                return null;
            }

            // Comparación exacta: el texto es solo un dato, no una orden.
            return BuscarExacto(nombreBuscado);
        }
    }
}
