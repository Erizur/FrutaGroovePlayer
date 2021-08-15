using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Threading;

namespace FrutaGroovePlayer
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        /// 
    [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            const string appName = "FrutaGroovePlayer";
            bool createdNew;

            var mutex = new Mutex(true, appName, out createdNew);

            if (args.Length == 0)
            {
                Application.Run(new Form1(null, false));
            }
            else
            {
                if (!createdNew)
                {
                    Application.Run(new Form1(args[0], true));
                }
                else
                {
                    Application.Run(new Form1(args[0], false));
                }
                
            }
        }
    }
}
