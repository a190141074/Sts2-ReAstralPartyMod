using System.Reflection;
using System.Runtime.Loader;
var baseDir = @"D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64";
AssemblyLoadContext.Default.Resolving += (_, name) => {
  var candidate = Path.Combine(baseDir, name.Name + ".dll");
  return File.Exists(candidate) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate) : null;
};
var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(baseDir, "sts2.dll"));
var type = asm.GetType("MegaCrit.Sts2.Core.Nodes.Relics.NRelic");
Console.WriteLine("=== NRelic methods ===");
var flags = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
foreach (var m in type!.GetMethods(flags).OrderBy(m => m.Name))
{
  var ps = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
  Console.WriteLine($"METHOD {m.ReturnType.Name} {m.Name}({ps})");
}
