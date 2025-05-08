using System.Collections.Generic;

namespace AliasUtilities
{
    public class AliasResolver
    {
        private readonly Dictionary<string, string> _aliasMap;

        public AliasResolver()
        {
            string aliasConfig = System.Configuration.ConfigurationManager.AppSettings["TerminalServerAliases"] ?? "";
            _aliasMap = ParseConfigString(aliasConfig);
        }

        private Dictionary<string, string> ParseConfigString(string configString)
        {
            // split the aliases into a map that allows us to find the alias for a given input
            // the input is the key, and the alias is the value
            var aliasMap = new Dictionary<string, string>();
            // format: "INPUT=Alias;INPUT2=Alias with spaces; INPUT3=Alias with spaces ,and commas"
            string[] aliases = configString.Split(';');
            foreach (string alias in aliases)
            {
                string[] aliasPair = alias.Split('=');
                if (aliasPair.Length == 2)
                {
                    string input = aliasPair[0].Trim();
                    string aliasValue = aliasPair[1].Trim();
                    if (!aliasMap.ContainsKey(input))
                    {
                        aliasMap.Add(input, aliasValue);
                    }
                }
            }
            return aliasMap;
        }

        public string Resolve(string name)
        {
            // if the name is in the alias map, return the alias value
            if (_aliasMap.ContainsKey(name))
            {
                return _aliasMap[name];
            }

            // if the name is not in the alias map, return the name as is
            return name;
        }
    }
}