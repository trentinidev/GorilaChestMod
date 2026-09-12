using Mono.Cecil;
using Mono.Cecil.Cil;

// Usage: dotnet run -- [assembly_valheim.dll] [GorilaChestMod.dll]
// Run this again after every Valheim update: it proves that everything the mod
// patches still exists, and that the call sites and field reads the transpilers
// rewrite are still there. Exit code 0 means the mod is still compatible.
string managed = @"S:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed";
string game = args.Length > 0 ? args[0] : Path.Combine(managed, "assembly_valheim.dll");
string mod = args.Length > 1
    ? args[1]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "bin", "Release", "GorilaChestMod.dll"));

Console.WriteLine($"game: {game}");
Console.WriteLine($"mod : {mod}");

var g = AssemblyDefinition.ReadAssembly(game).MainModule;
int fail = 0;

void Fail(string message)
{
    Console.WriteLine("FAIL " + message);
    fail++;
}

TypeDefinition Type(string name)
{
    var t = g.GetType(name) ?? AllTypes().FirstOrDefault(x => x.Name == name);
    if (t == null) Fail($"type {name} not found");
    return t;
}

MethodDefinition Method(string typeName, string name, string[] pars, bool quiet = false)
{
    var t = Type(typeName);
    if (t == null) return null;

    var candidates = t.Methods.Where(m => m.Name == name).ToList();
    var match = pars == null
        ? candidates.FirstOrDefault()
        : candidates.FirstOrDefault(m => m.Parameters.Select(p => p.ParameterType.Name).SequenceEqual(pars));

    if (match == null)
    {
        Fail($"{typeName}.{name}({(pars == null ? "any" : string.Join(",", pars))}) not found. Candidates: " +
             string.Join(" | ", candidates.Select(c => $"({string.Join(",", c.Parameters.Select(p => p.ParameterType.Name))})")));
    }
    else if (!quiet)
    {
        Console.WriteLine($"ok   {typeName}.{name}({(pars == null ? "" : string.Join(",", pars))})");
    }

    return match;
}

void Field(string typeName, string fieldName)
{
    var t = Type(typeName);
    if (t == null) return;
    if (t.Fields.Any(f => f.Name == fieldName)) Console.WriteLine($"ok   field {typeName}.{fieldName}");
    else Fail($"field {typeName}.{fieldName} not found");
}

int CallsTo(MethodDefinition method, MethodDefinition target) =>
    method?.Body == null || target == null
        ? 0
        : method.Body.Instructions.Count(i =>
            (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
            i.Operand is MethodReference mr && mr.FullName == target.FullName);

int ReadsOf(MethodDefinition method, string fieldName) =>
    method?.Body == null
        ? 0
        : method.Body.Instructions.Count(i =>
            i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference fr && fr.Name == fieldName);

Console.WriteLine("\n--- patch targets: craft from chests ---");
var haveReqItems = Method("Player", "HaveRequirementItems", new[] { "Recipe", "Boolean", "Int32", "Int32" });
var firstRequired = Method("Player", "GetFirstRequiredItem", new[] { "Inventory", "Recipe", "Int32", "Int32&", "Int32&", "Int32" });
var consume = Method("Player", "ConsumeResources", new[] { "Requirement[]", "Int32", "Int32", "Int32" });
var havePiece = Method("Player", "HaveRequirements", new[] { "Piece", "RequirementMode" });
Method("Player", "UpdatePlacement", new[] { "Boolean", "Single" });
var doCrafting = Method("InventoryGui", "DoCrafting", new[] { "Player" });
var setupReq = Method("InventoryGui", "SetupRequirement", new[] { "Transform", "Requirement", "Player", "Boolean", "Int32", "Int32" });
Method("Hud", "SetupPieceInfo", new[] { "Piece" });
Method("Container", "Awake", Array.Empty<string>());

Console.WriteLine("\n--- patch targets: chest stacks ---");
var addItem = Method("Inventory", "AddItem", new[] { "ItemData" });
Method("Inventory", "AddItem", new[] { "ItemData", "Int32", "Int32", "Int32", "Boolean" });
var updateGui = Method("InventoryGrid", "UpdateGui", new[] { "Player", "ItemData" });
Method("ItemDrop", "DropItem", new[] { "ItemData", "Int32", "Vector3", "Quaternion" });
Field("SharedData", "m_maxStackSize");

Console.WriteLine("\n--- patch targets: quick stack and networking ---");
Method("InventoryGui", "Update", Array.Empty<string>());
Method("InventoryGui", "OnDestroy", Array.Empty<string>());
Method("InventoryGui", "IsVisible", Array.Empty<string>());
Method("InventoryGrid", "UpdateGui", new[] { "Player", "ItemData" });
Method("Menu", "IsVisible", Array.Empty<string>());
Method("TextInput", "IsVisible", Array.Empty<string>());
Method("Chat", "HasFocus", Array.Empty<string>());
Field("InventoryGrid", "m_elements");
Field("InventoryElement", "m_amount");
Method("Inventory", "ContainsItemByName", new[] { "String" });
Method("Inventory", "MoveItemToThis", new[] { "Inventory", "ItemData" });
Method("Humanoid", "IsItemEquiped", new[] { "ItemData" });
Method("ZNet", "Awake", Array.Empty<string>());
Method("ZNet", "RPC_PeerInfo", new[] { "ZRpc", "ZPackage" });
Method("ZNet", "Shutdown", new[] { "Boolean" });
Method("ZNet", "IsServer", Array.Empty<string>());
Method("ZRoutedRpc", "InvokeRoutedRPC", new[] { "String", "Object[]" });
Method("ZRoutedRpc", "InvokeRoutedRPC", new[] { "Int64", "String", "Object[]" });
Field("InventoryGui", "m_moveItemEffects");

Console.WriteLine("\n--- patch targets: favorites ---");
var stackAll = Method("Inventory", "StackAll", new[] { "Inventory", "Boolean" });
Method("InventoryGui", "OnSelectedItem", new[] { "InventoryGrid", "ItemData", "Vector2i", "Modifier" });
Method("Inventory", "GetItemAt", new[] { "Int32", "Int32" });
Method("Inventory", "GetWidth", Array.Empty<string>());
Method("InventoryGrid", "GetInventory", Array.Empty<string>());
Field("ItemData", "m_customData");
Field("InventoryElement", "m_icon");
Field("InventoryGui", "m_dragGo");

var isEquipped = Method("Humanoid", "IsItemEquiped", new[] { "ItemData" }, quiet: true);
int equipChecks = CallsTo(stackAll, isEquipped);
if (equipChecks < 1) Fail("Inventory.StackAll no longer asks IsItemEquiped, favorites would not be honoured by the chest stack button");
else Console.WriteLine($"ok   Inventory.StackAll: {equipChecks} call(s) to Humanoid.IsItemEquiped");

Console.WriteLine("\n--- redirected calls still present ---");
var invCount = Method("Inventory", "CountItems", new[] { "String", "Int32", "Boolean" }, quiet: true);
var invHave = Method("Inventory", "HaveItem", new[] { "String", "Boolean" }, quiet: true);
var invGet = Method("Inventory", "GetItem", new[] { "String", "Int32", "Boolean" }, quiet: true);
var invRemove = Method("Inventory", "RemoveItem", new[] { "String", "Int32", "Int32", "Boolean" }, quiet: true);

void ExpectCalls(string label, MethodDefinition method, MethodDefinition target, int min)
{
    int n = CallsTo(method, target);
    if (n < min) Fail($"{label}: expected at least {min} call(s) to {target?.Name}, found {n}");
    else Console.WriteLine($"ok   {label}: {n} call(s) to Inventory.{target?.Name}");
}

ExpectCalls("HaveRequirementItems", haveReqItems, invCount, 1);
ExpectCalls("GetFirstRequiredItem", firstRequired, invCount, 1);
ExpectCalls("GetFirstRequiredItem", firstRequired, invGet, 1);
ExpectCalls("ConsumeResources", consume, invRemove, 1);
ExpectCalls("HaveRequirements(Piece)", havePiece, invHave, 1);
ExpectCalls("HaveRequirements(Piece)", havePiece, invCount, 1);
ExpectCalls("DoCrafting", doCrafting, invRemove, 1);
ExpectCalls("SetupRequirement", setupReq, invCount, 1);

Console.WriteLine("\n--- stack limit reads still present ---");
var inventoryType = Type("Inventory");
var readers = inventoryType == null
    ? new List<MethodDefinition>()
    : inventoryType.Methods.Where(m => ReadsOf(m, "m_maxStackSize") > 0).ToList();

if (readers.Count == 0) Fail("no Inventory method reads m_maxStackSize any more, chest stacks would be inactive");
else Console.WriteLine($"ok   {readers.Count} Inventory method(s) read m_maxStackSize: " +
                       string.Join(", ", readers.Select(m => m.Name).Distinct()));

// The load path is the one that would clamp saved chests back down.
var loadPath = inventoryType?.Methods.FirstOrDefault(m =>
    m.Name == "AddItem" && m.Parameters.Count > 5 && m.Parameters[0].ParameterType.Name == "Int32" && ReadsOf(m, "m_maxStackSize") > 0);
if (loadPath == null) Fail("the AddItem overload that loads a saved inventory no longer clamps by m_maxStackSize, check the load path by hand");
else Console.WriteLine($"ok   load path {loadPath.Name} with {loadPath.Parameters.Count} parameters reads m_maxStackSize");

int gridReads = ReadsOf(updateGui, "m_maxStackSize");
if (gridReads == 0) Fail("InventoryGrid.UpdateGui no longer reads m_maxStackSize, chest slots would show the vanilla limit");
else Console.WriteLine($"ok   InventoryGrid.UpdateGui: {gridReads} read(s) of m_maxStackSize");

Console.WriteLine("\n--- mod side signatures ---");
var m2 = AssemblyDefinition.ReadAssembly(mod).MainModule;

var bridge = m2.GetType("GorilaChestMod.InventoryBridge");
void CheckBridge(string bridgeName, MethodDefinition vanilla)
{
    if (vanilla == null || bridge == null) { Fail($"cannot check bridge {bridgeName}"); return; }
    var b = bridge.Methods.FirstOrDefault(x => x.Name == bridgeName);
    if (b == null) { Fail($"bridge {bridgeName} missing"); return; }

    var expected = new[] { "Inventory" }.Concat(vanilla.Parameters.Select(p => p.ParameterType.Name)).ToArray();
    var actual = b.Parameters.Select(p => p.ParameterType.Name).ToArray();

    if (expected.SequenceEqual(actual) && b.ReturnType.Name == vanilla.ReturnType.Name && b.IsStatic)
        Console.WriteLine($"ok   bridge {bridgeName}({string.Join(",", actual)}) -> {b.ReturnType.Name}");
    else
        Fail($"bridge {bridgeName}: parameters {string.Join(",", actual)} vs expected {string.Join(",", expected)}, " +
             $"return {b.ReturnType.Name} vs {vanilla.ReturnType.Name}, static {b.IsStatic}");
}

CheckBridge("CountItems", invCount);
CheckBridge("HaveItem", invHave);
CheckBridge("GetItem", invGet);
CheckBridge("RemoveItem", invRemove);

var stacks = m2.GetType("GorilaChestMod.ChestStacks");
var maxStackFor = stacks?.Methods.FirstOrDefault(m => m.Name == "MaxStackFor");
if (maxStackFor == null) Fail("ChestStacks.MaxStackFor missing");
else if (!maxStackFor.IsStatic ||
         maxStackFor.ReturnType.Name != "Int32" ||
         !maxStackFor.Parameters.Select(p => p.ParameterType.Name).SequenceEqual(new[] { "SharedData", "Inventory" }))
    Fail("ChestStacks.MaxStackFor must be static int (SharedData, Inventory) to replace the field read");
else Console.WriteLine("ok   ChestStacks.MaxStackFor(SharedData,Inventory) -> Int32");

Console.WriteLine(fail == 0 ? "\nALL CHECKS PASSED" : $"\n{fail} CHECK(S) FAILED");
return fail == 0 ? 0 : 1;

IEnumerable<TypeDefinition> AllTypes()
{
    IEnumerable<TypeDefinition> Walk(TypeDefinition t)
    {
        yield return t;
        foreach (var nested in t.NestedTypes)
            foreach (var deep in Walk(nested))
                yield return deep;
    }

    return g.Types.SelectMany(Walk);
}
