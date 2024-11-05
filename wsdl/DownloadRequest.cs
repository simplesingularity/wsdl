using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace wsdl
{
    public class DownloadRequest
    {
        public string GameId { get; set; }
        public string Id { get; set; }  
        public string RawData { get; set; }
        public string Path { get; set; }
    }
}
