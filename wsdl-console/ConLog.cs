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

        public void Log(string message, params string[] args)
        {
            Log(string.Format(message, args));
        }

        public void Log(string message, Exception exception)
        {
            Log(string.Format("Error: {0}, {1}", exception, exception.Message));
        }
    }
}
