using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class Program
{
    private const string BootstrapResource =
        "LSTool.Resources.Settings.ReleaseProfile.dat";

    private static readonly string[] RequiredPublicTypes =
    {
        "LSTool.Application",
        "LSTool.Tools.Beams.BeamRebar.BeamRebarCmd",
        "LSTool.Tools.Beams.InstallRebarBeamV2.InstallRebarBeamV2Cmd",
        "LSTool.Tools.Columns.ColumnRebar.ColumnRebarCmd",
        "LSTool.Tools.Generals.RebarInfomationParameter." +
        "RebarInfomationParameterCmd",
        "LSTool.Tools.Generals.SettingDiameters.RebarDatabasesCmd",
        "LSTool.Tools.Generals.SettingRebarStandard." +
        "SettingRebarStandardCmd"
    };

    private static int Main(string[] args)
    {
        if (args.Length == 0 || args.Length % 3 != 0)
        {
            Console.Error.WriteLine(
                "Usage: ObfuscationKernelTests " +
                "<RevitVersion> <source.dll> <protected.dll> [...]");
            return 2;
        }

        var failures = 0;
        for (var index = 0; index < args.Length; index += 3)
        {
            failures += VerifyPair(
                args[index],
                args[index + 1],
                args[index + 2]);
        }

        if (failures == 0)
        {
            Console.WriteLine(
                $"All {args.Length / 3} protected LSTools assemblies " +
                "passed metadata checks.");
            return 0;
        }

        Console.Error.WriteLine(
            $"{failures} protected assembly check(s) failed.");
        return 1;
    }

    private static int VerifyPair(
        string revitVersion,
        string sourcePath,
        string protectedPath)
    {
        try
        {
            var source = ReadAssembly(sourcePath);
            var protectedAssembly = ReadAssembly(protectedPath);

            Require(
                source.AssemblyName == protectedAssembly.AssemblyName,
                "assembly identity changed");
            Require(
                protectedAssembly.AssemblyName == "LSTool",
                "protected assembly is not LSTool");

            foreach (var typeName in RequiredPublicTypes)
            {
                Require(
                    protectedAssembly.PublicTypes.Contains(typeName),
                    $"required public type is missing: {typeName}");
            }

            Require(
                protectedAssembly.Resources.Contains(BootstrapResource),
                $"embedded bootstrap resource is missing: {BootstrapResource}");

            var renamedTypeCount = source.AllTypes
                .Except(protectedAssembly.AllTypes, StringComparer.Ordinal)
                .Count();
            Require(
                renamedTypeCount >= 20,
                $"only {renamedTypeCount} type names changed");

            Require(
                !source.FileHash.SequenceEqual(protectedAssembly.FileHash),
                "protected file hash matches source");

            Console.WriteLine(
                $"PASS R{revitVersion}: " +
                $"{renamedTypeCount} type names changed; " +
                $"{RequiredPublicTypes.Length} Revit entry types preserved.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"FAIL R{revitVersion}: {exception.Message}");
            return 1;
        }
    }

    private static AssemblyMetadata ReadAssembly(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Assembly was not found.", path);
        }

        using var stream = File.OpenRead(path);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
        {
            throw new InvalidDataException(
                $"Assembly has no managed metadata: {path}");
        }

        var reader = peReader.GetMetadataReader();
        var assemblyName = reader.GetString(
            reader.GetAssemblyDefinition().Name);
        var allTypes = new HashSet<string>(StringComparer.Ordinal);
        var publicTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in reader.TypeDefinitions)
        {
            var definition = reader.GetTypeDefinition(handle);
            var name = reader.GetString(definition.Name);
            if (name == "<Module>")
            {
                continue;
            }

            var typeName = GetFullTypeName(reader, handle);
            allTypes.Add(typeName);

            var visibility =
                definition.Attributes & TypeAttributes.VisibilityMask;
            if (visibility is TypeAttributes.Public or
                TypeAttributes.NestedPublic)
            {
                publicTypes.Add(typeName);
            }
        }

        var resources = reader.ManifestResources
            .Select(handle => reader.GetString(
                reader.GetManifestResource(handle).Name))
            .ToHashSet(StringComparer.Ordinal);

        return new AssemblyMetadata(
            assemblyName,
            allTypes,
            publicTypes,
            resources,
            System.Security.Cryptography.SHA256.HashData(
                File.ReadAllBytes(path)));
    }

    private static string GetFullTypeName(
        MetadataReader reader,
        TypeDefinitionHandle handle)
    {
        var definition = reader.GetTypeDefinition(handle);
        var name = reader.GetString(definition.Name);
        var declaringType = definition.GetDeclaringType();
        if (!declaringType.IsNil)
        {
            return GetFullTypeName(reader, declaringType) + "+" + name;
        }

        var typeNamespace = reader.GetString(definition.Namespace);
        return string.IsNullOrEmpty(typeNamespace)
            ? name
            : typeNamespace + "." + name;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    private sealed record AssemblyMetadata(
        string AssemblyName,
        HashSet<string> AllTypes,
        HashSet<string> PublicTypes,
        HashSet<string> Resources,
        byte[] FileHash);
}
