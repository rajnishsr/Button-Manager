using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hca.SSOSolution.LASSO.UI
{
    public static class Globals
    {
        public static String argValue { get; set; }

        public void getName()
        {
            Console.Clear();
            Console.Write("Enter name: ");
            MyName = Console.ReadLine();
        }
    }
}
