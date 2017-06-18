using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotaTextGame
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            User.con = new MySql.Data.MySqlClient.MySqlConnection(args[1]);

            Main main = new Main();
            main.bw_DoWork(args[0]);
            Console.ReadLine();
        }
    }
}
