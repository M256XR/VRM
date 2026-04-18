using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class PhysBoneApiDumper
{
    [MenuItem("Tools/VRM Wallpaper/Dump PhysBone API")]
    public static void Dump()
    {
        List<string> lines = new List<string>();
        lines.Add("=== PhysBone API Dump Start ===");

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                     .Where(a => a.GetName().Name.IndexOf("VRC", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            lines.Add($"Assembly: {assembly.FullName}");
            Type physBoneRootInterface = GetLoadableTypes(assembly).FirstOrDefault(t => t.FullName == "VRC.Dynamics.IPhysBoneRoot");
            foreach (Type type in GetLoadableTypes(assembly)
                         .Where(t => t != null && t.FullName != null &&
                                     t.FullName.IndexOf("PhysBone", StringComparison.OrdinalIgnoreCase) >= 0)
                         .OrderBy(t => t.FullName))
            {
                DumpType(type, lines);
            }

            if (physBoneRootInterface != null)
            {
                lines.Add("IPhysBoneRoot implementors:");
                foreach (Type type in AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(GetLoadableTypes)
                             .Where(t => t != null && t.FullName != null && physBoneRootInterface.IsAssignableFrom(t) && t != physBoneRootInterface)
                             .OrderBy(t => t.FullName))
                {
                    lines.Add($"  IMPLEMENTOR {type.FullName} base={type.BaseType?.FullName} interfaces={string.Join(",", type.GetInterfaces().Select(i => i.FullName))}");
                    DumpType(type, lines);
                }
            }
        }

        lines.Add("=== PhysBone API Dump End ===");

        string outputPath = Path.Combine(Application.dataPath, "..", "BuildSmoke", "physbone_api_dump_clean.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllLines(outputPath, lines);

        Debug.Log($"PhysBone API dump written to {outputPath}");
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(t => t != null).ToArray();
        }
    }

    private static void DumpType(Type type, List<string> lines)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.DeclaredOnly;

        lines.Add($"TYPE {type.FullName} base={type.BaseType?.FullName}");

        foreach (FieldInfo field in type.GetFields(flags).OrderBy(f => f.Name))
        {
            lines.Add($"  FIELD {Access(field)} {field.FieldType.FullName} {field.Name}");
        }

        foreach (PropertyInfo property in type.GetProperties(flags).OrderBy(p => p.Name))
        {
            lines.Add($"  PROP {property.PropertyType.FullName} {property.Name} get={property.CanRead} set={property.CanWrite}");
        }

        foreach (MethodInfo method in type.GetMethods(flags)
                     .Where(m => !m.IsSpecialName)
                     .OrderBy(m => m.Name))
        {
            string parameters = string.Join(", ",
                method.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"));
            lines.Add($"  METHOD {Access(method)} {method.ReturnType.FullName} {method.Name}({parameters})");
        }

        if (type.FullName == "VRC.Dynamics.PhysBoneRoot")
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                         .Where(m => !m.IsSpecialName)
                         .OrderBy(m => m.Name))
            {
                string parameters = string.Join(", ",
                    method.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"));
                lines.Add($"  ALLMETHOD {Access(method)} {method.ReturnType.FullName} {method.Name}({parameters})");
            }

            lines.Add("  INTERFACES " + string.Join(", ", type.GetInterfaces().Select(i => i.FullName)));
        }

        foreach (Type nested in type.GetNestedTypes(flags).OrderBy(t => t.FullName))
        {
            DumpType(nested, lines);
        }
    }

    private static string Access(MethodBase method)
    {
        if (method.IsPublic) return "public";
        if (method.IsFamily) return "protected";
        if (method.IsAssembly) return "internal";
        if (method.IsPrivate) return "private";
        return method.Attributes.ToString();
    }

    private static string Access(FieldInfo field)
    {
        if (field.IsPublic) return "public";
        if (field.IsFamily) return "protected";
        if (field.IsAssembly) return "internal";
        if (field.IsPrivate) return "private";
        return field.Attributes.ToString();
    }
}
