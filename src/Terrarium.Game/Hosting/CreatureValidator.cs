// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using OrganismBase;

namespace Terrarium.Game.Hosting;

/// <summary>
/// Structured result of assembly validation.
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
/// Static analysis of creature assemblies before loading.
/// Inspects IL metadata for forbidden patterns (P/Invoke, unsafe, forbidden namespaces),
/// validates creature base type (Animal or Plant), and checks required attributes.
/// </summary>
public sealed class CreatureValidator
{
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
    /// Validates a creature assembly from raw bytes.
    /// Performs metadata-level IL checks, then loads to verify base class and attributes.
    /// </summary>
    public ValidationResult Validate(byte[] assemblyBytes)
    {
        if (assemblyBytes == null || assemblyBytes.Length == 0)
            return ValidationResult.Fail(["Assembly bytes are null or empty."]);

        var errors = new List<string>();

        try
        {
            using var stream = new MemoryStream(assemblyBytes);
            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata)
                return ValidationResult.Fail(["Assembly does not contain managed metadata."]);

            var reader = peReader.GetMetadataReader();
            CheckForPInvoke(reader, errors);
            CheckForbiddenNamespaces(reader, errors);
            CheckInheritanceChain(reader, errors);
        }
        catch (BadImageFormatException ex)
        {
            return ValidationResult.Fail([$"Invalid assembly format: {ex.Message}"]);
        }

        if (errors.Count == 0)
        {
            try
            {
                var assembly = Assembly.Load(assemblyBytes);
                CheckCreatureBaseType(assembly, errors);
                CheckRequiredAttributes(assembly, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to inspect assembly: {ex.Message}");
            }
        }

        return errors.Count == 0
            ? ValidationResult.Pass()
            : ValidationResult.Fail(errors);
    }

    private static void CheckForPInvoke(MetadataReader reader, List<string> errors)
    {
        foreach (var methodHandle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(methodHandle);
            if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                var name = reader.GetString(method.Name);
                var declaringType = reader.GetTypeDefinition(method.GetDeclaringType());
                var typeName = reader.GetString(declaringType.Name);
                var typeNamespace = reader.GetString(declaringType.Namespace);
                errors.Add($"Forbidden P/Invoke declaration: {typeNamespace}.{typeName}.{name}");
            }

            if ((method.ImplAttributes & MethodImplAttributes.Unmanaged) != 0 ||
                (method.ImplAttributes & MethodImplAttributes.Native) != 0)
            {
                var name = reader.GetString(method.Name);
                errors.Add($"Forbidden unmanaged/native method: {name}");
            }
        }
    }

    private static void CheckForbiddenNamespaces(MetadataReader reader, List<string> errors)
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
    }

    private static readonly HashSet<string> AllowedBaseTypes = new(StringComparer.Ordinal)
    {
        "OrganismBase.Animal",
        "OrganismBase.Plant",
    };

    private static void CheckInheritanceChain(MetadataReader reader, List<string> errors)
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
            errors.Add("No public non-abstract type found that extends OrganismBase.Animal or OrganismBase.Plant.");
        }
    }

    private static bool HasAllowedBaseType(MetadataReader reader, TypeDefinition typeDef)
    {
        var baseTypeHandle = typeDef.BaseType;

        for (var depth = 0; depth < 20 && !baseTypeHandle.IsNil; depth++)
        {
            switch (baseTypeHandle.Kind)
            {
                case HandleKind.TypeReference:
                    var typeRef = reader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
                    var baseNs = reader.GetString(typeRef.Namespace);
                    var baseName = reader.GetString(typeRef.Name);
                    return AllowedBaseTypes.Contains($"{baseNs}.{baseName}");

                case HandleKind.TypeDefinition:
                    var baseTypeDef = reader.GetTypeDefinition((TypeDefinitionHandle)baseTypeHandle);
                    var ns = reader.GetString(baseTypeDef.Namespace);
                    var name = reader.GetString(baseTypeDef.Name);
                    if (AllowedBaseTypes.Contains($"{ns}.{name}")) return true;
                    baseTypeHandle = baseTypeDef.BaseType;
                    break;

                default:
                    return false;
            }
        }

        return false;
    }

    private static void CheckCreatureBaseType(Assembly assembly, List<string> errors)
    {
        var creatureTypes = assembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        (typeof(Animal).IsAssignableFrom(t) || typeof(Plant).IsAssignableFrom(t)))
            .ToList();

        if (creatureTypes.Count == 0)
        {
            errors.Add("Assembly must contain at least one non-abstract class inheriting from Animal or Plant.");
        }
    }

    private static void CheckRequiredAttributes(Assembly assembly, List<string> errors)
    {
        var authorAttr = assembly.GetCustomAttributes()
            .FirstOrDefault(a => a.GetType() == typeof(AuthorInformationAttribute));

        if (authorAttr == null)
        {
            errors.Add("Assembly must have an [assembly: AuthorInformation] attribute.");
        }
    }
}
