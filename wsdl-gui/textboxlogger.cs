using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using wsdl;

namespace wsdl_gui
{
    internal class textboxlogger : ILogger
    {
        private TextBox textbox;
        private delegate void append(string text);
        public textboxlogger(TextBox tb)
        {
            this.textbox = tb;
        }
        public void Log(string message)
        {
            if(this.textbox.InvokeRequired )
            {
                this.textbox.Parent.Invoke( new append(Log), message);
            }else
            {
                this.textbox.AppendText(message + Environment.NewLine);
            }
        }

        public void Log(string message, params string[] args)
        {
            Log(string.Format(message, args));
        }

        public void Log(string message, Exception exception)
        {
            Log(string.Format(message + ": {0}", exception));
        }
    }
}
