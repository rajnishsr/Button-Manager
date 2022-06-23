using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Hca.SSOSolution.LASSO.UI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static String argPath { get; set; }
        public static String argParam { get; set; }


        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {

                int i = -1;
                for (int j = 0; j < args.Length; j++)
                {
                    if (args[j].ToLower().Contains("appparams"))
                    {
                        i = j;
                    }
                }
                if (i > -1)
                {

                    for (int j = 0; j < i; j++)
                    {
                        if (Program.argPath != null)
                        {
                            Program.argPath = Program.argPath + " " + args[j];
                        }
                        else
                        {
                            Program.argPath = args[j];
                        }
                    }

                    Program.argParam = args[i];
                }
                else
                {
                    Program.argParam = "AppParams = DSC";
                    for (int j = 0; j < args.Length; j++)
                    {
                        if (Program.argPath != null)
                        {
                            Program.argPath = Program.argPath + " " + args[j];
                        }
                        else
                        {
                            Program.argPath = args[j];
                        }
                    }

                }
            }
            else
            {
                Program.argPath = null;
                Program.argParam = "AppParams = DSC";
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LASSOForm());
        }
    }

}
