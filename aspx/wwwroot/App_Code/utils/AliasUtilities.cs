using System.Collections.Generic;
using RAWeb.Server.Utilities;

namespace RAWebServer.Utilities {
    public class AliasResolver {
        private readonly Dictionary<string, string> _aliasMap;

        public AliasResolver() {
            var aliasConfig = PoliciesManager.RawPolicies["TerminalServerAliases"] ?? "";
            _aliasMap = ParseConfigString(aliasConfig);
        }

        private Dictionary<string, string> ParseConfigString(string configString) {
            // split the aliases into a map that allows us to find the alias for a given input
            // the input is the key, and the alias is the value
            var aliasMap = new Dictionary<string, string>();
            // format: "INPUT=Alias;INPUT2=Alias with spaces; INPUT3=Alias with spaces ,and commas"
            var aliases = configString.Split(';');
            foreach (var alias in aliases) {
                var aliasPair = alias.Split('=');
                if (aliasPair.Length == 2) {
                    var input = aliasPair[0].Trim();
                    var aliasValue = aliasPair[1].Trim();
                    if (!aliasMap.ContainsKey(input)) {
                        aliasMap.Add(input, aliasValue);
                    }
                }
            }
            return aliasMap;
        }

        public string Resolve(string name) {
            // if the name is in the alias map, return the alias value
            if (_aliasMap.ContainsKey(name)) {
                return _aliasMap[name];
            }

            // if the name is not in the alias map, return the name as is
            return name;
        }
    }
}
