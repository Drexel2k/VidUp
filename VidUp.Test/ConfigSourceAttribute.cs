using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drexel.VidUp.Test
{
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class ConfigSourceAttribute : System.Attribute
    {
        private string source;

        public string Source
        {
            get => this.source;
        }
        public ConfigSourceAttribute(string source)
        {
            this.source = source;
        }
    }
}