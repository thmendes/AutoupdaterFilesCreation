using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static string messageTitle = "Package Creator";
    static string pakFolderName = "zips";
    static string pakExtension = ".zip";

    static void Main(string[] args)
    {
        Console.WriteLine($"{messageTitle}");

        string sourcePath = SelectFolder("Enter the source folder path:");
        string outputPath = SelectFolder("Enter the output folder path:");

        string version = SelectVersion();

        if (!ValidateDirectory(sourcePath, true) || !ValidateDirectory(outputPath, false))
            return;

        Console.WriteLine("Generating package files...");

        GenerateFiles(sourcePath, outputPath, version);

        Console.WriteLine("Operation has completed!");
    }

    static string SelectFolder(string message)
    {
        Console.WriteLine(message);
        string path = Console.ReadLine();

        while (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            Console.WriteLine("Invalid folder path. Please try again.");
            path = Console.ReadLine();
        }

        return path;
    }

    static string SelectVersion()
    {
        Console.WriteLine("Enter version number:");
        string version = Console.ReadLine();

        while (string.IsNullOrEmpty(version) || !int.TryParse(version, out _))
        {
            Console.WriteLine("Invalid version. Please enter a valid number:");
            version = Console.ReadLine();
        }

        return version;
    }

    static bool ValidateDirectory(string path, bool shouldNotBeEmpty)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Folder '{path}' not found.");
            return false;
        }

        var dirInfo = new DirectoryInfo(path);

        if (shouldNotBeEmpty && !dirInfo.EnumerateFiles().Any() && !dirInfo.EnumerateDirectories().Any())
        {
            Console.WriteLine($"Folder '{path}' is empty.");
            return false;
        }

        if (!shouldNotBeEmpty && (dirInfo.EnumerateFiles().Any() || dirInfo.EnumerateDirectories().Any()))
        {
            Console.WriteLine($"Output folder '{path}' is not empty.");
            return false;
        }

        return true;
    }

    static void GenerateFiles(string sourcePath, string outputPath, string version)
    {
        sourcePath = sourcePath.TrimEnd('\\');
        outputPath = outputPath.TrimEnd('\\');

        // Create version.txt
        File.WriteAllText(Path.Combine(outputPath, "version.txt"), version, Encoding.Unicode);

        var fileList = GetFilesRecursive(sourcePath);

        // Create filelist.txt
        using (var writer = new StreamWriter(Path.Combine(outputPath, "filelist.txt"), false, Encoding.Unicode))
        {
            foreach (var file in fileList)
            {
                writer.WriteLine($"{GetFileHash(file)}\t{GetRelativePath(sourcePath, file)}");
            }
        }

        // Create zip files
        foreach (var file in fileList)
        {
            var relativePath = GetRelativePath(sourcePath, file);
            var zipOutputPath = Path.Combine(outputPath, pakFolderName, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(zipOutputPath));

            using (var zipArchive = ZipFile.Open($"{zipOutputPath}{pakExtension}", ZipArchiveMode.Create))
            {
                zipArchive.CreateEntryFromFile(file, relativePath);
            }
        }
    }

    static string GetRelativePath(string basePath, string fullPath)
    {
        return fullPath.Substring(basePath.Length + 1);
    }

    static List<string> GetFilesRecursive(string initialDir)
    {
        var result = new List<string>();
        var dirs = new Stack<string>();
        dirs.Push(initialDir);

        while (dirs.Count > 0)
        {
            var dir = dirs.Pop();

            try
            {
                result.AddRange(Directory.GetFiles(dir));
                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    dirs.Push(subDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directory {dir}: {ex.Message}");
            }
        }

        return result;
    }

    static string GetFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = sha256.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
