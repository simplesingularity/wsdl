using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wsdl;

namespace wsdl_console
{
    internal class ConLog : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine("[Internal Debug]" + message);
        }
    }
}
