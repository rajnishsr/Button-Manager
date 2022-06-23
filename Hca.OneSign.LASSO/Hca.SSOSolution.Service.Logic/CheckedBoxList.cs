using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Hca.SSOSolution.Service.Logic
{
    public class CheckedBoxListClass
    {
        public CheckedListBox chkLstBox;

        private static CheckedBoxListClass instance;

        private CheckedBoxListClass() { }

        public static CheckedBoxListClass Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CheckedBoxListClass();
                }
                return instance;
            }
        }
    }
}
