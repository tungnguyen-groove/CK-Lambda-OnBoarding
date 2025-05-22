using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace SnowflakeItemMaster.Tests.Helpers
{
    public static class LambdaBuilder
    {
        public static async Task<byte[]> BuildLambdaPackage(string projectPath, ILogger logger)
        {
            string tempDir = null;
            try
            {
                logger.LogInformation("Building Lambda function from project: {ProjectPath}", projectPath);

                // Validate project file exists
                if (!File.Exists(projectPath))
                {
                    throw new FileNotFoundException($"Project file not found: {projectPath}");
                }

                // Create temp directory for build output
                tempDir = Path.Combine(Path.GetTempPath(), "lambda-build-" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                logger.LogInformation("Created temporary build directory: {TempDir}", tempDir);

                // Build the project with Lambda-specific parameters
                var buildProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"publish \"{projectPath}\" -c Release -o \"{tempDir}\" --runtime linux-x64 --self-contained false",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(projectPath)
                    }
                };

                logger.LogInformation("Starting build process with command: dotnet {Arguments}", buildProcess.StartInfo.Arguments);

                buildProcess.Start();

                var outputTask = buildProcess.StandardOutput.ReadToEndAsync();
                var errorTask = buildProcess.StandardError.ReadToEndAsync();

                await buildProcess.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                logger.LogInformation("Build output: {Output}", output);

                if (!string.IsNullOrEmpty(error))
                {
                    logger.LogWarning("Build stderr: {Error}", error);
                }

                if (buildProcess.ExitCode != 0)
                {
                    throw new Exception($"Build failed with exit code {buildProcess.ExitCode}. Error: {error}");
                }

                logger.LogInformation("Project built successfully");

                // List files in build directory for debugging
                var files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                logger.LogInformation("Build output contains {FileCount} files:", files.Length);
                foreach (var file in files.Take(10)) // Log first 10 files
                {
                    logger.LogDebug("  - {File}", Path.GetFileName(file));
                }

                // Create zip file from build output
                var zipPath = Path.Combine(Path.GetTempPath(), $"lambda-package-{Guid.NewGuid()}.zip");
                logger.LogInformation("Creating zip file at: {ZipPath}", zipPath);

                // Create zip with proper structure (files at root level, not in subdirectory)
                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        var relativePath = Path.GetRelativePath(tempDir, file);
                        // Skip any directory entries and ensure forward slashes for Lambda
                        var entryName = relativePath.Replace('\\', '/');

                        logger.LogDebug("Adding file to zip: {EntryName}", entryName);
                        archive.CreateEntryFromFile(file, entryName);
                    }
                }

                // Verify zip was created
                if (!File.Exists(zipPath))
                {
                    throw new Exception("Failed to create zip file");
                }

                // Read zip file
                var zipBytes = await File.ReadAllBytesAsync(zipPath);
                logger.LogInformation("Zip file created successfully, size: {Size} bytes", zipBytes.Length);

                // Verify zip content
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    logger.LogInformation("Zip contains {EntryCount} entries:", archive.Entries.Count);
                    foreach (var entry in archive.Entries.Take(5)) // Log first 5 entries
                    {
                        logger.LogDebug("  - {EntryName} ({Size} bytes)", entry.FullName, entry.Length);
                    }
                }

                // Cleanup zip file
                File.Delete(zipPath);

                return zipBytes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error building Lambda package from {ProjectPath}", projectPath);
                throw;
            }
            finally
            {
                // Cleanup temp directory
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                        logger.LogInformation("Temporary directory cleaned up: {TempDir}", tempDir);
                    }
                    catch (Exception cleanupEx)
                    {
                        logger.LogWarning(cleanupEx, "Failed to cleanup temporary directory: {TempDir}", tempDir);
                    }
                }
            }
        }
    }
}