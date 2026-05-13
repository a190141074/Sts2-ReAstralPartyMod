using System;
using System.Linq;
using System.Reflection;
class Program {
  static void Main() {
    var asm = Assembly.LoadFrom(@"D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll");
    var t = asm.GetType("MegaCrit.Sts2.Core.Nodes.Relics.NRelic");
    Console.WriteLine(t?.FullName ?? "<null>");
    foreach (var m in t!.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).OrderBy(x => x.Name)) {
      var ps = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
      Console.WriteLine($"M|{m.Name}|{m.ReturnType.Name}|{ps}");
    }
    foreach (var f in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).OrderBy(x => x.Name)) {
      Console.WriteLine($"F|{f.Name}|{f.FieldType.FullName}");
    }
  }
}
