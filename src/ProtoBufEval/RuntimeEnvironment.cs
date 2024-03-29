using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ProtoBufEval;

internal enum OperatingSystem
{
    Unknown,
    Windows,
    Linux,
    OSX
}

internal class RuntimeEnvironment
{
    public static OperatingSystem Platform
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OperatingSystem.OSX;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OperatingSystem.Linux;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OperatingSystem.Windows;

            return OperatingSystem.Unknown;
        }
    }

    public static string NativeBinary
    {
        get
        {
            var binaryDir = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine(NativeBinariesDir, OS)
                : Path.Combine(NativeBinariesDir, $"{OS}-{Arch}");
            Directory.CreateDirectory(binaryDir);
            var compressedBinary = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine(NativeBinariesDir, $"{OS}.zip")
                : Path.Combine(NativeBinariesDir, $"{OS}-{Arch}.zip");
            EnsureDependencyExists(compressedBinary);
            var binaryFilePath = Path.Combine(binaryDir, BinaryName);
            if (File.Exists(binaryFilePath))
                File.Delete(binaryFilePath);
            ZipFile.ExtractToDirectory(compressedBinary, binaryDir);
            return binaryFilePath;
        }
    }

    private static string NativeBinariesDir
    {
        get
        {
            var assemblyDir = Path.GetDirectoryName(typeof(RuntimeEnvironment).Assembly.Location);
            Console.WriteLine($"Assembly Directory: {assemblyDir}");
            return Path.Combine(assemblyDir, "tools", "protoeval");
        }
    }

    private static string BinaryName
    {
        get => Platform switch
        {
            OperatingSystem.Windows => "protoeval.exe",
            _ => "protoeval"
        };
    }

    private static string OS
    {
        get
        {
            return Platform switch
            {
                OperatingSystem.Windows => "win",
                OperatingSystem.Linux => "linux",
                OperatingSystem.OSX => "osx",
                _ => throw new Exception("Unsupported OS")
            };
        }
    }

    private static string Arch
    {
        get
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 or Architecture.X86 => "x86",
                Architecture.Arm or Architecture.Arm64 => "arm",
                _ => throw new Exception("Unsupported OS architecture")
            };
        }
    }

    private static void EnsureDependencyExists(string path)
    {
        if (!File.Exists(path))
            throw new ProtoBufEvalMissingDependencyException($"Missing dependency for ProtoBufEval: {path}");
    }
}