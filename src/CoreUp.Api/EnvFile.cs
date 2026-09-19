namespace CoreUp.Api;

internal static class EnvFile
{
    public static void Load()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, ".env");
            if (File.Exists(path))
            {
                foreach (var rawLine in File.ReadLines(path))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith('#')) continue;
                    var separator = line.IndexOf('=');
                    if (separator <= 0) continue;
                    var key = line[..separator].Trim();
                    var value = line[(separator + 1)..].Trim().Trim('"', '\'');
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                        Environment.SetEnvironmentVariable(key, value);
                }
                return;
            }
            directory = directory.Parent;
        }
    }
}
