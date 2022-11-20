using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netfetch
{
    public interface IInfoFetch
    {
        public string OSName { get; }
        public string Host { get; }
        public string Kernel { get; }
        public string Uptime { get; }
        public string Resolution { get; }
        public string CPU { get; }
        public string GPU { get; }
    }
}
