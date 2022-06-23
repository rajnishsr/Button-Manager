using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hca.SSOSolution.Service.Logic
{
    public interface ISingleSignOnService
    {
        //Business facade pattern
        //Inversion of control
        //Do tests
        void Load(out string message);
        void Save();
        string ComposeHelpMessage();
    }
}
