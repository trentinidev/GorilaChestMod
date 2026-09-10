using Mono.Cecil;
using Mono.Cecil.Cil;

// Usage: dotnet run -- [assembly_valheim.dll] [CraftFromChests.dll]
// Run this again after every Valheim update: it proves that every method the mod
// patches still exists and still contains the Inventory call sites that the
// transpilers rewrite. Exit code 0 means the mod is still compatible.
string game = args.Length > 0
    ? args[0]
    : @"S:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll";
string mod = args.Length > 1
    ? args[1]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "bin", "Release", "CraftFromChests.dll"));

Console.WriteLine($"game: {game}");
Console.WriteLine($"mod : {mod}");

var g = AssemblyDefinition.ReadAssembly(game).MainModule;
int fail = 0;

MethodDefinition Find(string type, string name, string[] pars)
{
    var t = g.GetType(type) ?? g.Types.FirstOrDefault(x => x.Name == type);
    if (t == null) { Console.WriteLine($"FAIL type {type} not found"); fail++; return null; }
    var cands = t.Methods.Where(m => m.Name == name).ToList();
    var m = cands.FirstOrDefault(m => m.Parameters.Select(p => p.ParameterType.Name).SequenceEqual(pars));
    if (m == null)
    {
        Console.WriteLine($"FAIL {type}.{name}({string.Join(",", pars)}) not found. Candidates: " +
            string.Join(" | ", cands.Select(c => $"{c.Name}({string.Join(",", c.Parameters.Select(p => p.ParameterType.Name))})")));
        fail++;
    }
    else Console.WriteLine($"ok   target {type}.{name}({string.Join(",", pars)})  [private={m.IsPrivate}]");
    return m;
}

// --- 1. the six patch targets exist with the expected signature
var targets = new List<(MethodDefinition m, string label)>();
targets.Add((Find("Player", "HaveRequirementItems", new[]{"Recipe","Boolean","Int32","Int32"}), "Player.HaveRequirementItems"));
targets.Add((Find("Player", "GetFirstRequiredItem", new[]{"Inventory","Recipe","Int32","Int32&","Int32&","Int32"}), "Player.GetFirstRequiredItem"));
targets.Add((Find("Player", "ConsumeResources", new[]{"Requirement[]","Int32","Int32","Int32"}), "Player.ConsumeResources"));
targets.Add((Find("Player", "HaveRequirements", new[]{"Piece","RequirementMode"}), "Player.HaveRequirements(Piece)"));
targets.Add((Find("Player", "UpdatePlacement", new[]{"Boolean","Single"}), "Player.UpdatePlacement"));
targets.Add((Find("InventoryGui", "DoCrafting", new[]{"Player"}), "InventoryGui.DoCrafting"));
targets.Add((Find("InventoryGui", "SetupRequirement", new[]{"Transform","Requirement","Player","Boolean","Int32","Int32"}), "InventoryGui.SetupRequirement"));
targets.Add((Find("Hud", "SetupPieceInfo", new[]{"Piece"}), "Hud.SetupPieceInfo"));
targets.Add((Find("Container", "Awake", Array.Empty<string>()), "Container.Awake"));

// --- 2. the Inventory methods the transpilers swap
var invCount  = Find("Inventory", "CountItems", new[]{"String","Int32","Boolean"});
var invHave   = Find("Inventory", "HaveItem",   new[]{"String","Boolean"});
var invGet    = Find("Inventory", "GetItem",    new[]{"String","Int32","Boolean"});
var invRemove = Find("Inventory", "RemoveItem", new[]{"String","Int32","Int32","Boolean"});

// --- 3. each patched method really contains the call sites the transpiler looks for
void ExpectCalls(string label, string type, string name, string[] pars, params (MethodDefinition target, int min)[] expect)
{
    var m = Find(type, name, pars);
    if (m?.Body == null) { Console.WriteLine($"FAIL {label} has no body"); fail++; return; }
    foreach (var (target, min) in expect)
    {
        if (target == null) continue;
        int n = m.Body.Instructions.Count(i =>
            (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
            i.Operand is MethodReference mr && mr.FullName == target.FullName);
        if (n < min) { Console.WriteLine($"FAIL {label}: expected >={min} call(s) to {target.Name}, found {n}"); fail++; }
        else Console.WriteLine($"ok   {label}: {n} call(s) to Inventory.{target.Name}");
    }
}

Console.WriteLine("--- call sites ---");
ExpectCalls("HaveRequirementItems", "Player", "HaveRequirementItems", new[]{"Recipe","Boolean","Int32","Int32"}, (invCount,1));
ExpectCalls("GetFirstRequiredItem", "Player", "GetFirstRequiredItem", new[]{"Inventory","Recipe","Int32","Int32&","Int32&","Int32"}, (invCount,1),(invGet,1));
ExpectCalls("ConsumeResources", "Player", "ConsumeResources", new[]{"Requirement[]","Int32","Int32","Int32"}, (invRemove,1));
ExpectCalls("HaveRequirements(Piece)", "Player", "HaveRequirements", new[]{"Piece","RequirementMode"}, (invHave,1),(invCount,1));
ExpectCalls("DoCrafting", "InventoryGui", "DoCrafting", new[]{"Player"}, (invRemove,1));
ExpectCalls("SetupRequirement", "InventoryGui", "SetupRequirement", new[]{"Transform","Requirement","Player","Boolean","Int32","Int32"}, (invCount,1));

// --- 4. bridge signatures line up: static(Inventory, <same params>) with same return type
Console.WriteLine("--- bridge signatures ---");
var m2 = AssemblyDefinition.ReadAssembly(mod).MainModule;
var bridge = m2.GetType("CraftFromChests.InventoryBridge");
void CheckBridge(string bridgeName, MethodDefinition vanilla)
{
    if (vanilla == null) return;
    var b = bridge.Methods.FirstOrDefault(x => x.Name == bridgeName);
    if (b == null) { Console.WriteLine($"FAIL bridge {bridgeName} missing"); fail++; return; }
    var expected = new[]{"Inventory"}.Concat(vanilla.Parameters.Select(p => p.ParameterType.Name)).ToArray();
    var actual = b.Parameters.Select(p => p.ParameterType.Name).ToArray();
    bool okPars = expected.SequenceEqual(actual);
    bool okRet = b.ReturnType.Name == vanilla.ReturnType.Name;
    bool okStatic = b.IsStatic;
    if (okPars && okRet && okStatic) Console.WriteLine($"ok   bridge {bridgeName}({string.Join(",", actual)}) -> {b.ReturnType.Name}");
    else { Console.WriteLine($"FAIL bridge {bridgeName}: pars {string.Join(",",actual)} vs expected {string.Join(",",expected)}; ret {b.ReturnType.Name} vs {vanilla.ReturnType.Name}; static={okStatic}"); fail++; }
}
CheckBridge("CountItems", invCount);
CheckBridge("HaveItem", invHave);
CheckBridge("GetItem", invGet);
CheckBridge("RemoveItem", invRemove);

Console.WriteLine(fail == 0 ? "\nALL CHECKS PASSED" : $"\n{fail} CHECK(S) FAILED");
return fail == 0 ? 0 : 1;
