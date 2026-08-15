using System.IO;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

internal static class TestProjectInfo
{
    private static readonly Lazy<string> ProjectVersionValue =
        new(() => ReadRootFile("VERSION"));

    private static readonly Lazy<string> PowerToysVersionValue =
        new(() => ReadRootFile("POWERTOYS_VERSION"));

    public static string ProjectVersion =>
        ProjectVersionValue.Value;

    public static string PowerToysVersion =>
        PowerToysVersionValue.Value;

    private static string ReadRootFile(
        string fileName)
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            var path =
                Path.Combine(
                    directory.FullName,
                    fileName);

            if (File.Exists(path))
            {
                var value =
                    File.ReadAllText(path)
                        .Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException(
                        $"{fileName} is empty: {path}");
                }

                return value;
            }

            directory =
                directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate repository root file '{fileName}' "
            + $"starting from '{AppContext.BaseDirectory}'.");
    }
}