using System;
using System.Linq;
using System.Reflection;
class Program {
  static void Dump(Type t) {
    Console.WriteLine($"TYPE {t.FullName}");
    foreach (var c in t.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static)) Console.WriteLine("CTOR " + c);
    foreach (var p in t.GetProperties(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static)) Console.WriteLine($"PROP {p.Name} : {p.PropertyType.FullName}");
    foreach (var m in t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static).Where(m => !m.IsSpecialName)) Console.WriteLine("METH " + m);
    Console.WriteLine();
  }
  static void Main() {
    var asm = Assembly.LoadFrom(@"D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll");
    Dump(asm.GetType("MegaCrit.Sts2.Core.Runs.CardCreationOptions")!);
    Dump(asm.GetType("MegaCrit.Sts2.Core.Rewards.CardReward")!);
    Dump(asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardRewardSelectionScreen")!);
  }
}
