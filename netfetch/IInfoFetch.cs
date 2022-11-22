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
        public IEnumerable<string> Resolution { get; }
        public IEnumerable<string> CPU { get; }
        public IEnumerable<string> GPU { get; }
        public string Memory { get; }
    }
}
