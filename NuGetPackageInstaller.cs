using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace Quriz.NuGet
{
    public static class NuGetPackageInstaller
    {
        // Helpful Links:
        // https://learn.microsoft.com/en-us/nuget/api/overview
        
        private const string NuGetPackageDownloadUrlTemplate = "https://api.nuget.org/v3-flatcontainer/{id-lower}/{version-lower}/{id-lower}.{version-lower}.nupkg";

        /// <summary>
        /// Installs a NuGet package without its dependencies. They must be installed manually also with this method.
        /// </summary>
        /// <param name="packageId">NuGet package ID</param>
        /// <param name="packageVersion">Version of the package</param>
        /// <param name="isEditorOnly">Should this package be downloaded into the "Plugins/Editor" folder for editor use only or just to the "Plugins" folder?</param>
        /// <param name="refreshAfterInstall">Should Unity's asset database be refreshed after installation?</param>
        /// <exception cref="WebException">When package download didn't finish successfully.</exception>
        /// <exception cref="InvalidPathException">When package has no folder for target framework .NET Standard 2.0 and 2.1</exception>
        public static void InstallNuGetEditorPackage(string packageId, string packageVersion, bool isEditorOnly = false, bool refreshAfterInstall = true)
        {
            var targetFolder = isEditorOnly ? "Plugins/Editor" : "Plugins";
            var targetFolderPath = Path.Combine(Application.dataPath, targetFolder, packageId);

            // Create plugin folder if it doesn't exist yet
            Directory.CreateDirectory(targetFolderPath);

            var nugetExtractPath = Path.Combine(Application.temporaryCachePath, $"{packageId}-{packageVersion}");
            var nugetArchivePath = Path.Combine(Application.temporaryCachePath, $"{packageId}-{packageVersion}.nupkg");

            // Download NuGet package
            using var request = GetRequestWithHandler(GetPackageUrl(packageId, packageVersion), new DownloadHandlerFile(nugetArchivePath));
            request.SendWebRequest();

            while (!request.isDone)
                Thread.Sleep(100);

            if (request.result != UnityWebRequest.Result.Success)
                throw new WebException($"Error in UnityWebRequest: {request.result}, {request.error}");
            
            // Extract NuGet package
            ZipFile.ExtractToDirectory(nugetArchivePath, nugetExtractPath, true);

            var netStandard20Path = Path.Combine(nugetExtractPath, "lib/netstandard2.0");
            var netStandard21Path = Path.Combine(nugetExtractPath, "lib/netstandard2.1");

            // Try to get correct directory path
            string dllDirPath;
            if (Directory.Exists(netStandard20Path)) dllDirPath = netStandard20Path;
            else if (Directory.Exists(netStandard21Path)) dllDirPath = netStandard21Path;
            else throw new InvalidPathException($"Package {packageId} {packageVersion} doesn't support .NET Standard 2.0 or 2.1");

            // Move all DLLs from correct target framework folder to target folder
            var dllFiles = Directory.GetFiles(dllDirPath, "*.dll");
            MoveFiles(dllFiles, targetFolderPath);
            
            // Move some files from base directory to target folder
            var allowedFileEndings = new[] { ".txt", ".md", "license" };
            var otherFiles = Directory.EnumerateFiles(nugetExtractPath)
                .Where(file => allowedFileEndings.Any(file.ToLower().EndsWith)).ToArray();
            MoveFiles(otherFiles, targetFolderPath);

            // Delete temp files
            File.Delete(nugetArchivePath);
            Directory.Delete(nugetExtractPath, true);

            // Optionally refresh Unity's asset database after installation
            if (refreshAfterInstall)
                AssetDatabase.Refresh();
        }

        /// <summary>
        /// Move specified files to targetFolder.
        /// </summary>
        /// <param name="files">Full file paths for the files that will be moved</param>
        /// <param name="targetFolder">The full path to the target folder where the files will be moved</param>
        private static void MoveFiles(IEnumerable<string> files, string targetFolder)
        {
            foreach (var file in files)
            {
                var targetFilePath = Path.Combine(targetFolder, Path.GetFileName(file));
                if (!File.Exists(targetFilePath))
                    File.Move(file, targetFilePath);
            }
        }

        private static string GetPackageUrl(string packageId, string packageVersion)
            => NuGetPackageDownloadUrlTemplate
                .Replace("{id-lower}", packageId.ToLowerInvariant())
                .Replace("{version-lower}", packageVersion.ToLowerInvariant());
        
        private static UnityWebRequest GetRequestWithHandler(string url, DownloadHandler downloadHandler)
            => new(url, UnityWebRequest.kHttpVerbGET, downloadHandler, null);
    }
}