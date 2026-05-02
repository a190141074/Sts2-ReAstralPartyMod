using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

var gameDir = @"D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64";
var alc = new AssemblyLoadContext("inspect-rest-props", true);
alc.Resolving += (ctx, name) => {
    var candidate = Path.Combine(gameDir, name.Name + ".dll");
    return File.Exists(candidate) ? ctx.LoadFromAssemblyPath(candidate) : null;
};
var asm = alc.LoadFromAssemblyPath(Path.Combine(gameDir, "sts2.dll"));
var t = asm.GetType("MegaCrit.Sts2.Core.Entities.RestSite.RestSiteOption")!;
foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
{
    Console.WriteLine($"PROP {prop.Name} type={prop.PropertyType.FullName} getter={prop.GetMethod?.Attributes} setter={prop.SetMethod?.Attributes}");
}
foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
{
    Console.WriteLine($"FIELD {field.FieldType.FullName} {field.Name}");
}
