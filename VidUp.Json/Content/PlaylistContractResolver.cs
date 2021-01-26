using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace Drexel.VidUp.Json.Content
{
    public class PlaylistContractResolver : DefaultContractResolver
    {
        private Dictionary<string, string> PropertyMappings { get; set; }

        public PlaylistContractResolver()
        {
            this.PropertyMappings = new Dictionary<string, string>
            {
                {"title", "name"},
            };
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            string resolvedName = null;
            var resolved = this.PropertyMappings.TryGetValue(propertyName, out resolvedName);
            return (resolved) ? resolvedName : base.ResolvePropertyName(propertyName);
        }
    }
}