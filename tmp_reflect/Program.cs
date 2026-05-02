using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
var gameDir = @"D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64";
var alc = new AssemblyLoadContext("inspect-shovel", true);
alc.Resolving += (ctx, name) => {
  var candidate = Path.Combine(gameDir, name.Name + ".dll");
  return File.Exists(candidate) ? ctx.LoadFromAssemblyPath(candidate) : null;
};
var asm = alc.LoadFromAssemblyPath(Path.Combine(gameDir, "sts2.dll"));
foreach (var t in asm.GetTypes().Where(t => t.Name.Equals("Shovel", StringComparison.OrdinalIgnoreCase) || t.FullName!.Contains("Shovel", StringComparison.OrdinalIgnoreCase)).OrderBy(t => t.FullName))
{
  Console.WriteLine(t.FullName);
  foreach (var m in t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly).OrderBy(m => m.Name))
  {
    Console.WriteLine("  " + m.Name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ") => " + m.ReturnType.Name);
  }
}
