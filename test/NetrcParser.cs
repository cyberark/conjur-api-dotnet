namespace Conjur.Test;

/// <summary>
/// Minimal parser for ~/.netrc files, matching the format used by conjur-api-go.
/// Supports machine/login/password stanzas only.
/// </summary>
internal static class NetrcParser
{
    internal record NetrcEntry(string Machine, string Login, string Password);

    /// <summary>
    /// Parses a netrc file and returns all entries found.
    /// Tokens are whitespace/newline delimited per the netrc spec.
    /// </summary>
    internal static IReadOnlyList<NetrcEntry> Parse(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var tokens = content.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

        var entries = new List<NetrcEntry>();
        string machine = null, login = null, password = null;

        for (var i = 0; i < tokens.Length; i++)
        {
            switch (tokens[i])
            {
                case "machine":
                    if (machine != null)
                        entries.Add(new NetrcEntry(machine, login, password));
                    machine = tokens[++i];
                    login = null;
                    password = null;
                    break;
                case "login":
                    login = tokens[++i];
                    break;
                case "password":
                    password = tokens[++i];
                    break;
                case "default":
                    if (machine != null)
                        entries.Add(new NetrcEntry(machine, login, password));
                    machine = "default";
                    login = null;
                    password = null;
                    break;
                case "macdef":
                    i++;
                    break;
            }
        }

        if (machine != null)
            entries.Add(new NetrcEntry(machine, login, password));

        return entries;
    }

    /// <summary>
    /// Returns the first entry whose machine matches the host of the given appliance URL,
    /// or the first entry overall if no match is found (mirrors Go netrc behaviour).
    /// </summary>
    internal static NetrcEntry FindEntry(string filePath, string applianceUrl)
    {
        var entries = Parse(filePath);
        if (entries.Count == 0)
            return null;

        var host = new Uri(applianceUrl).Host;
        return entries.FirstOrDefault(e => e.Machine.Contains(host, StringComparison.OrdinalIgnoreCase))
               ?? entries[0];
    }
}