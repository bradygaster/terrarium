// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.Logging;

namespace Terrarium.Game.Hosting;

/// <summary>
/// Result of assembly validation.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Reasons { get; }

    private ValidationResult(bool isValid, IReadOnlyList<string> reasons)
    {
        IsValid = isValid;
        Reasons = reasons;
    }

    public static ValidationResult Pass() => new(true, Array.Empty<string>());
    public static ValidationResult Fail(IReadOnlyList<string> reasons) => new(false, reasons);
}

/// <summary>
/// Managed IL validator that replaces the legacy C++ AsmCheck tool.
/// Uses <see cref="System.Reflection.Metadata"/> to inspect assemblies without
/// loading them, checking for forbidden P/Invoke declarations, forbidden
/// namespace usage, and validating inheritance chains.
/// </summary>
public sealed class AssemblyValidator
{
    private readonly ILogger<AssemblyValidator> _logger;

    /// <summary>
    /// Namespaces that organism assemblies are forbidden from using.
    /// </summary>
    private static readonly string[] ForbiddenNamespaces =
    [
        "System.IO",
        "System.Net",
        "System.Diagnostics.Process",
        "System.Runtime.InteropServices",
        "System.Reflection.Emit",
        "System.Threading",
        "System.Security",
        "Microsoft.Win32",
    ];

    /// <summary>
    /// Fully qualified base type names that organisms must extend.
    /// </summary>
    private static readonly HashSet<string> AllowedBaseTypes = new(StringComparer.Ordinal)
    {
        "OrganismBase.Animal",
        "OrganismBase.Plant",
    };

    public AssemblyValidator(ILogger<AssemblyValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates an assembly at the given path without loading it into the runtime.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly DLL to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> describing whether the assembly is valid.</returns>
    public ValidationResult Validate(string assemblyPath)
    {
        var errors = new List<string>();

        if (!File.Exists(assemblyPath))
        {
            return ValidationResult.Fail([$"Assembly file not found: {assemblyPath}"]);
        }

        try
        {
            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata)
            {
                return ValidationResult.Fail(["Assembly does not contain managed metadata."]);
            }

            var metadataReader = peReader.GetMetadataReader();

            CheckForPInvoke(metadataReader, errors);
            CheckForbiddenNamespaces(metadataReader, errors);
            CheckInheritanceChain(metadataReader, errors);
        }
        catch (BadImageFormatException ex)
        {
            _logger.LogWarning(ex, "Assembly has invalid format: {Path}", assemblyPath);
            return ValidationResult.Fail([$"Invalid assembly format: {ex.Message}"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating assembly: {Path}", assemblyPath);
            return ValidationResult.Fail([$"Validation error: {ex.Message}"]);
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Assembly validation failed for {Path} with {Count} error(s).",
                assemblyPath, errors.Count);
        }
        else
        {
            _logger.LogDebug("Assembly validation passed for {Path}.", assemblyPath);
        }

        return errors.Count == 0
            ? ValidationResult.Pass()
            : ValidationResult.Fail(errors);
    }

    /// <summary>
    /// Checks for any P/Invoke (DllImport) declarations which are forbidden in organism assemblies.
    /// </summary>
    private void CheckForPInvoke(MetadataReader reader, List<string> errors)
    {
        foreach (var methodDefHandle in reader.MethodDefinitions)
        {
            var methodDef = reader.GetMethodDefinition(methodDefHandle);

            if ((methodDef.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                var methodName = reader.GetString(methodDef.Name);
                var declaringType = reader.GetTypeDefinition(methodDef.GetDeclaringType());
                var typeName = reader.GetString(declaringType.Name);
                var typeNamespace = reader.GetString(declaringType.Namespace);

                errors.Add(
                    $"Forbidden P/Invoke declaration: {typeNamespace}.{typeName}.{methodName}");
            }

            var implAttributes = methodDef.ImplAttributes;
            if ((implAttributes & MethodImplAttributes.Unmanaged) != 0 ||
                (implAttributes & MethodImplAttributes.Native) != 0)
            {
                var methodName = reader.GetString(methodDef.Name);
                errors.Add($"Forbidden unmanaged/native method: {methodName}");
            }
        }
    }

    /// <summary>
    /// Scans type references for usage of forbidden namespaces.
    /// </summary>
    private void CheckForbiddenNamespaces(MetadataReader reader, List<string> errors)
    {
        foreach (var typeRefHandle in reader.TypeReferences)
        {
            var typeRef = reader.GetTypeReference(typeRefHandle);
            var ns = reader.GetString(typeRef.Namespace);
            var name = reader.GetString(typeRef.Name);

            foreach (var forbidden in ForbiddenNamespaces)
            {
                if (ns.Equals(forbidden, StringComparison.Ordinal) ||
                    ns.StartsWith(forbidden + ".", StringComparison.Ordinal))
                {
                    errors.Add($"Forbidden namespace reference: {ns}.{name}");
                    break;
                }
            }
        }

        foreach (var typeDefHandle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(typeDefHandle);
            var ns = reader.GetString(typeDef.Namespace);

            foreach (var forbidden in ForbiddenNamespaces)
            {
                if (ns.Equals(forbidden, StringComparison.Ordinal) ||
                    ns.StartsWith(forbidden + ".", StringComparison.Ordinal))
                {
                    var name = reader.GetString(typeDef.Name);
                    errors.Add($"Type declared in forbidden namespace: {ns}.{name}");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Validates that at least one public type in the assembly extends Animal or Plant.
    /// </summary>
    private void CheckInheritanceChain(MetadataReader reader, List<string> errors)
    {
        var hasValidBase = false;

        foreach (var typeDefHandle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(typeDefHandle);

            if ((typeDef.Attributes & TypeAttributes.Public) == 0) continue;
            if ((typeDef.Attributes & TypeAttributes.Abstract) != 0) continue;

            if (HasAllowedBaseType(reader, typeDef))
            {
                hasValidBase = true;
                break;
            }
        }

        if (!hasValidBase)
        {
            errors.Add(
                "No public non-abstract type found that extends OrganismBase.Animal or OrganismBase.Plant.");
        }
    }

    /// <summary>
    /// Walks the base type chain of a type definition to see if it ultimately
    /// extends one of the allowed base types.
    /// </summary>
    private static bool HasAllowedBaseType(MetadataReader reader, TypeDefinition typeDef)
    {
        var baseTypeHandle = typeDef.BaseType;

        for (var depth = 0; depth < 20 && !baseTypeHandle.IsNil; depth++)
        {
            string? baseNamespace;
            string? baseName;

            switch (baseTypeHandle.Kind)
            {
                case HandleKind.TypeReference:
                    var typeRef = reader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
                    baseNamespace = reader.GetString(typeRef.Namespace);
                    baseName = reader.GetString(typeRef.Name);

                    if (AllowedBaseTypes.Contains($"{baseNamespace}.{baseName}"))
                        return true;

                    return false;

                case HandleKind.TypeDefinition:
                    var baseTypeDef = reader.GetTypeDefinition((TypeDefinitionHandle)baseTypeHandle);
                    baseNamespace = reader.GetString(baseTypeDef.Namespace);
                    baseName = reader.GetString(baseTypeDef.Name);

                    if (AllowedBaseTypes.Contains($"{baseNamespace}.{baseName}"))
                        return true;

                    baseTypeHandle = baseTypeDef.BaseType;
                    break;

                default:
                    return false;
            }
        }

        return false;
    }
}
