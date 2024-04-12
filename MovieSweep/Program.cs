using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MovieFolderCleanup
{
    class Program
    {
        static readonly string[] videoExtensions = { ".mp4", ".mkv", ".avi", ".mov" };
        static readonly string[] subtitleExtensions = { ".srt", ".sub", ".idx" };
        static readonly string successLogPath = "success.log";
        static readonly string errorLogPath = "error.log";

        static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
            var version = assembly.GetName().Version;

            Console.WriteLine($"{assemblyName} - Version {version}");
            Console.WriteLine(new string('=', 30));

            string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
            bool verbose = args.Contains("-v");
            bool autoRename = args.Contains("-a");

            LogMessage(successLogPath, $"Starting cleanup in directory: {directoryPath}");
            //LogMessage(errorLogPath, $"Starting cleanup in directory: {directoryPath}");

            if (args.Any(arg => Directory.Exists(arg)))
            {
                directoryPath = args.First(arg => Directory.Exists(arg));
            }

            DirectoryInfo di = new DirectoryInfo(directoryPath);
            Console.WriteLine($"Cleaning up movie folders in: {directoryPath}");

            DirectoryInfo[] directories = di.GetDirectories();
            for (int i = 0; i < directories.Length; i++)
            {
                DirectoryInfo folder = directories[i];

                try
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Processing folder: {folder.Name}");
                    }

                    string newFolderName = CleanFolderName(folder.Name);
                    string newFolderPath = Path.Combine(directoryPath, newFolderName);

                    if (!newFolderName.Equals(folder.Name, StringComparison.OrdinalIgnoreCase) && !Directory.Exists(newFolderPath))
                    {
                        if (autoRename || PromptForConfirmation($"Rename {folder.Name} to {newFolderName}?"))
                        {
                            Directory.Move(folder.FullName, newFolderPath);
                            if (verbose)
                            {
                                Console.WriteLine($"Renamed to: {newFolderName}");
                            }
                            LogMessage(successLogPath, $"Renamed folder '{folder.Name}' to '{newFolderName}'.");
                            folder = new DirectoryInfo(newFolderPath); // Update the folder reference
                        }
                    }

                    // Proceed to clean up the folder
                    CleanupFolder(folder, autoRename, verbose);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while processing {folder.Name}: {ex.Message}");
                    LogMessage(errorLogPath, $"Error processing folder '{folder.Name}': {ex.Message}");
                }
            }

            Console.WriteLine("Cleanup completed.");
        }

        static string CleanFolderName(string folderName)
        {
            // Regex to capture a title that starts at the beginning and a year that follows it.
            // It handles cases where the year might be followed by additional tags.
            var match = Regex.Match(folderName, @"^(.+?)\s*\(\s*(\d{4})\s*\)");
            if (match.Success)
            {
                string movieName = match.Groups[1].Value.Trim();
                movieName = Regex.Replace(movieName, @"[\.\-_]+", " ").Trim(); // Replaces dots, underscores, or hyphens with spaces
                movieName = Regex.Replace(movieName, @"\s+", " "); // Replaces multiple spaces with a single space
                string year = match.Groups[2].Value;

                return $"{movieName} ({year})";
            }
            else
            {
                // If the folder name does not contain a year in parentheses, then it's not in the correct format.
                // We should strip all additional tags in that case.
                var matchNoYear = Regex.Match(folderName, @"^(.+?)\b(\d{4})\b");
                if (matchNoYear.Success)
                {
                    string titlePart = matchNoYear.Groups[1].Value.Trim();
                    titlePart = Regex.Replace(titlePart, @"[\.\-_]+", " ").Trim(); // Replaces dots, underscores, or hyphens with spaces
                    titlePart = Regex.Replace(titlePart, @"\s+", " "); // Replaces multiple spaces with a single space
                    string yearPart = matchNoYear.Groups[2].Value;

                    return $"{titlePart} ({yearPart})";
                }
            }
            return folderName; // If no match, return the original name
        }


        static void CleanupFolder(DirectoryInfo folder, bool autoRename, bool verbose)
        {
            string backupRootPath = AppDomain.CurrentDomain.BaseDirectory;

            // Handle files that match "sample"
            var sampleFiles = folder.GetFiles("*sample*", SearchOption.AllDirectories);
            foreach (var file in sampleFiles)
            {
                MoveToBackup(file, backupRootPath, folder, autoRename, verbose);
            }

            // Handle directories that match "sample"
            var sampleDirectories = folder.GetDirectories("*sample*", SearchOption.AllDirectories);
            foreach (var dir in sampleDirectories)
            {
                MoveToBackup(dir, backupRootPath, folder, autoRename, verbose);
            }

            // Handle non-movie related files
            var nonMovieFiles = folder.GetFiles().Where(f => !IsMovieRelated(f.Extension)).ToList();
            if (autoRename)
            {
                foreach (var file in nonMovieFiles)
                {
                    MoveToBackup(file, backupRootPath, folder, autoRename, verbose);
                }
            }
            else if (nonMovieFiles.Any() && PromptForConfirmation($"Found {nonMovieFiles.Count} non-movie files in {folder.Name}. Delete them?"))
            {
                foreach (var file in nonMovieFiles)
                {
                    file.Delete();
                    LogMessage(successLogPath, $"Deleted non-movie file: {file.FullName}");
                    if (verbose)
                    {
                        Console.WriteLine($"Deleted non-movie file: {file.FullName}");
                    }
                }
            }
        }

        static void MoveToBackup(FileSystemInfo item, string applicationRootPath, DirectoryInfo movieFolder, bool autoRename, bool verbose)
        {
            if (autoRename)
            {
                string relativePath = Path.GetRelativePath(movieFolder.FullName, item.FullName);
                string backupFolderName = CleanFolderName(movieFolder.Name);
                string backupRoot = Path.Combine(applicationRootPath, ".MovieCleanupBackup", backupFolderName);
                string destinationPath = Path.Combine(backupRoot, relativePath);

                // Check if the destination is a file and already exists.
                if (item is FileInfo && File.Exists(destinationPath))
                {
                    // Generate a new unique file path
                    destinationPath = EnsureUniqueFileName(destinationPath);
                }
                else if (item is DirectoryInfo && Directory.Exists(destinationPath))
                {
                    // If it's a directory and it exists, we simply skip the creation and move on to moving contents.
                    if (verbose)
                    {
                        Console.WriteLine($"Backup directory '{destinationPath}' already exists. Contents will be added into it.");
                    }
                }
                else
                {
                    // If the directory does not exist, create the directory structure.
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                }

                try
                {
                    if (item is FileInfo fileInfo)
                    {
                        fileInfo.MoveTo(destinationPath, true); // Overwrites if the file already exists.
                        LogMessage(successLogPath, $"Moved file '{fileInfo.FullName}' to '{destinationPath}'.");
                    }
                    else if (item is DirectoryInfo dirInfo)
                    {
                        // If the directory already exists, we move its contents instead of the directory itself.
                        if (!Directory.Exists(destinationPath))
                        {
                            Directory.CreateDirectory(destinationPath);
                        }
                        foreach (var dirItem in dirInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                        {
                            string targetPath = Path.Combine(destinationPath, Path.GetRelativePath(dirInfo.FullName, dirItem.FullName));
                            if (dirItem is FileInfo)
                            {
                                File.Move(dirItem.FullName, targetPath, true);
                            }
                            else if (dirItem is DirectoryInfo)
                            {
                                Directory.Move(dirItem.FullName, targetPath);
                            }
                        }
                        dirInfo.Delete(true); // Delete the source directory after moving its contents.
                        LogMessage(successLogPath, $"Moved contents of directory '{dirInfo.FullName}' to '{destinationPath}'.");
                    }
                }
                catch (Exception ex)
                {
                    string error = $"Failed to move '{item.FullName}' to '{destinationPath}' - {ex.Message}";
                    LogMessage(errorLogPath, error);
                    if (verbose)
                    {
                        Console.WriteLine(error);
                    }
                }

                if (verbose)
                {
                    Console.WriteLine($"Moved '{item.Name}' to backup location '{destinationPath}'.");
                }
            }
        }

        static string EnsureUniqueFileName(string path)
        {
            string directory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            int counter = 1;

            string newFullPath = path;
            while (File.Exists(newFullPath))
            {
                string tempFileName = $"{fileNameWithoutExtension} ({counter++}){extension}";
                newFullPath = Path.Combine(directory, tempFileName);
            }
            return newFullPath;
        }

        static string EnsureUniqueDirectoryName(string path)
        {
            int counter = 1;
            string newFullPath = path;
            while (Directory.Exists(newFullPath))
            {
                newFullPath = $"{path} ({counter++})";
            }
            return newFullPath;
        }

        static bool IsMovieRelated(string extension)
        {
            return videoExtensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase)) ||
                   subtitleExtensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }

        static void LogMessage(string filePath, string message)
        {
            File.AppendAllText(filePath, $"{message}{Environment.NewLine}");
        }

        static bool PromptForConfirmation(string message)
        {
            Console.WriteLine(message + " [y/n]");
            string input = Console.ReadLine().Trim().ToLower();
            return input == "y";
        }
    }
}
