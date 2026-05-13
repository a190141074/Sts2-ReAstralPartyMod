using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
var assemblyPath = @"D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";
var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
var resolver = new PathAssemblyResolver(Directory.GetFiles(runtimeDir, "*.dll").Concat([assemblyPath]));
using var mlc = new MetadataLoadContext(resolver);
var asm = mlc.LoadFromAssemblyPath(assemblyPath);
var type = asm.GetType("MegaCrit.Sts2.Core.Models.CardModel")!;
foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(p => p.Name.Contains("Description") || p.Name.Contains("Title")))
{
    Console.WriteLine($"PROPERTY\t{prop.PropertyType.FullName}\t{prop.Name}\tgetter={(prop.GetMethod != null ? prop.GetMethod.Name : "<none>")}");
}
