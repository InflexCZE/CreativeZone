using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CreativeZone.Utils;
using HarmonyLib;
using ModestTree;
using Service;
using Service.Localization;
using Log = CreativeZone.Utils.Log;

namespace CreativeZone
{
    public class HarmonyReplaceAttribute : Attribute
    { }

    public class HarmonyPropertyAttribute : Attribute
    {
        public string Target { get; set; }

        public HarmonyPropertyAttribute()
        {
            this.Target = null;
        }

        public HarmonyPropertyAttribute(string target)
        {
            this.Target = target;
        }
    }

    readonly struct ServicePatch
    {
        public readonly MethodInfo Target;
        public readonly PatchProcessor Patch;

        public ServicePatch(MethodInfo target, PatchProcessor patch)
        {
            this.Patch = patch;
            this.Target = target;
        }

        public MethodBase GetImplementedInterface(Type @interface)
        {
            var map = this.Target.DeclaringType.GetInterfaceMap(@interface);
            var targetIndex = Array.IndexOf(map.TargetMethods, this.Target);

            if (targetIndex < 0)
                return null;

            return map.InterfaceMethods[targetIndex];
        }
    }

    public class CreativeService<TInstance, TService>
        where TInstance : CreativeService<TInstance, TService>
    {
        public static TInstance Instance;
        public static bool[] StubIndirections; //TODO: Doesn't support multi-threading
        
        public TService Vanilla { get; private set; }

        public CreativeService()
        {
            if(Instance != null)
            {
                throw new Exception(typeof(TInstance).FullName + " is singleton");
            }

            Instance = (TInstance)(object)this;
        }

        private const BindingFlags InstanceBinding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void Install(Harmony harmony, object vanilla, Type vanillaInterfaceType)
        {
            Instance = Activator.CreateInstance<TInstance>();

            var vanillaImplType = vanilla.GetType();
            var fullInterfaceType = typeof(TService);

            Log.Debug.PrintLine($"Installing {Instance.GetType()} -> {fullInterfaceType} -> {vanillaImplType} -> {vanillaInterfaceType}");

            var stubs = new List<MethodBase>();
            var patches = new Dictionary<MethodBase, ServicePatch>();
            foreach (var method in typeof(TInstance).GetMethods(InstanceBinding))
            {
                var replace = method.GetCustomAttribute<HarmonyReplaceAttribute>();
                if(replace != null)
                {
                    var patch = GetPatch();
                    int stubId = stubs.Count;

                    var interfaceMethod = patch.GetImplementedInterface(vanillaInterfaceType);
                    if(interfaceMethod != null)
                    {
                        stubs.Add(interfaceMethod);
                    }
                    else
                    {
                        //This is not method from vanilla interface
                        var extendedInterfaceMethod = patch.Target.FindMatchingMethod(fullInterfaceType.GetAllInterfaceMethods(), false);
                        if(extendedInterfaceMethod != null)
                        {
                            //We can proxy it via extended interface
                            stubs.Add(extendedInterfaceMethod);
                        }
                        else
                        {
                            //Or don't proxy it at all if there is no match in extended interface either
                            stubId = -1;
                        }

                    }

                    patch.VanillaReplace(method, stubId);
                    continue;
                }

                //TODO: Fix for `Vanilla` call
                var prefix = method.GetCustomAttribute<HarmonyPrefix>();
                if (prefix != null)
                {
                    GetPatch().Patch.AddPrefix(new HarmonyMethod(method));
                    continue;
                }

                var postfix = method.GetCustomAttribute<HarmonyPostfix>();
                if (postfix != null)
                {
                    GetPatch().Patch.AddPostfix(new HarmonyMethod(method));
                    continue;
                }

                var finalizer = method.GetCustomAttribute<HarmonyFinalizer>();
                if (finalizer != null)
                {
                    GetPatch().Patch.AddFinalizer(new HarmonyMethod(method));
                    continue;
                }

                ServicePatch GetPatch()
                {
                    var target = method.FindMatchingMethod(vanillaImplType.GetMethods(InstanceBinding), true, IsTypeMatching);

                    //Path real implementation, not V-table stub
                    target = target.GetDeclaredMember();

                    if (patches.TryGetValue(target, out var patch) == false)
                    {
                        patch = new ServicePatch(target, harmony.CreateProcessor(target));
                        patches.Add(target, patch);
                    }

                    return patch;
                    
                    bool IsTypeMatching(Type real, Type general)
                    {
                        if(real == general)
                            return true;

                        if(real.IsClass && general == typeof(object))
                            return true;

                        if(real.IsByRef && (general == typeof(IntPtr) || general == typeof(void*)))
                            return true;

                        return false;
                    }
                }
            }

            foreach(var (_, patch) in patches)
            {
                patch.Patch.Patch();
            }

            if(stubs.Count > 0 || vanillaInterfaceType != fullInterfaceType)
            {
                StubIndirections = new bool[stubs.Count];

                var proxy = PatchHelpers.CreateDynamicType($"{fullInterfaceType.Name}_Proxy");
                proxy.SetParent(typeof(object));
                proxy.AddInterfaceImplementation(fullInterfaceType);

                var stubsField = AccessTools.Field(typeof(CreativeService<TInstance, TService>), "StubIndirections");
                var vanillaInstance = proxy.DefineField("__Vanilla", vanillaImplType, FieldAttributes.Public | FieldAttributes.InitOnly);

                var proxyDelegates = new List<(Delegate Delegate, FieldInfo Field)>();
                var vanillaMethods = vanillaInterfaceType.GetAllInterfaceMethods().ToHashSet();
                var declaredVanillaMethods = vanillaImplType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var method in fullInterfaceType.GetAllInterfaceMethods())
                {
                    var args = method.GetParameters();
                    var proxyMethod = proxy.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual);
                    proxyMethod.SetReturnType(method.ReturnType);
                    proxyMethod.SetParameters(args.Select(x => x.ParameterType).ToArray());
                    proxy.DefineMethodOverride(proxyMethod, method);

                    var IL = proxyMethod.GetILGenerator();

                    var stubId = stubs.IndexOf(method);
                    if(stubId >= 0)
                    {
                        //Set stub mark
                        IL.Emit(OpCodes.Ldsfld, stubsField);
                        IL.Emit(OpCodes.Ldc_I4, stubId);
                        IL.Emit(OpCodes.Ldc_I4_1);
                        IL.Emit(OpCodes.Stelem_I1);
                    }

                    MethodInfo directCallTarget = null;
                    var isInterfaceCall = vanillaMethods.Contains(method);

                    if(isInterfaceCall == false)
                    {
                        directCallTarget = method.FindMatchingMethod(declaredVanillaMethods, true);

                        if (directCallTarget.IsAssemblyPublic() == false)
                        {
                            //Note: Nasty hack follows!
                            //Dynamic type has to follow all visibility rules normal assembly would so it can't call private methods directly
                            //Runtime delegates can reflect, call and expose private members, but can't instantiate interface
                            //=> Call proxy delegate inside proxy type to expose private method via public interface. We need to go deeper :)
                            var delegateType = DelegateCreator.NewDelegateType(directCallTarget, unboundInstanceCall: true);
                            var proxyDelegate = Delegate.CreateDelegate(delegateType, directCallTarget);

                            var fieldName = $"__ProxyDelegate_{directCallTarget.Name}_{proxyDelegates.Count}";
                            var delegateField = proxy.DefineField(fieldName, delegateType, FieldAttributes.Private| FieldAttributes.InitOnly);

                            IL.Emit(OpCodes.Ldarg_0); //This
                            IL.Emit(OpCodes.Ldfld, delegateField);

                            proxyDelegates.Add((proxyDelegate, delegateField));
                            directCallTarget = delegateType.GetMethod("Invoke");
                        }
                    }

                    if(directCallTarget == null || directCallTarget.IsStatic == false)
                    {
                        IL.Emit(OpCodes.Ldarg_0); //This
                        IL.Emit(OpCodes.Ldfld, vanillaInstance);
                    }
                    
                    var argCount = args.Length;
                    for(int i = 1; i <= argCount; i++)
                    {
                        IL.Emit(OpCodes.Ldarg, i);
                    }

                    if (isInterfaceCall)
                    {
                        IL.Emit(OpCodes.Callvirt, method);
                    }
                    else
                    {
                        IL.Emit(OpCodes.Call, directCallTarget);
                    }

                    IL.Emit(OpCodes.Ret);
                }

                var ctorArgTypes = new[] {vanillaImplType}.Concat(proxyDelegates.Select(x => x.Field.FieldType));
                var ctor = proxy.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, ctorArgTypes.ToArray());
                var cIL = ctor.GetILGenerator();
                cIL.Emit(OpCodes.Ldarg_0);
                cIL.Emit(OpCodes.Call, proxy.BaseType.GetConstructor(Type.EmptyTypes));
                cIL.Emit(OpCodes.Ldarg_0);
                cIL.Emit(OpCodes.Ldarg_1);
                cIL.Emit(OpCodes.Stfld, vanillaInstance);

                for(int i = 0; i < proxyDelegates.Count; i++)
                {
                    cIL.Emit(OpCodes.Ldarg_0);
                    cIL.Emit(OpCodes.Ldarg, i + 2);
                    cIL.Emit(OpCodes.Stfld, proxyDelegates[i].Field);
                }

                cIL.Emit(OpCodes.Ret);

                var ctorArgs = new object[] {vanilla}.Concat(proxyDelegates.Select(x => x.Delegate));
                Instance.Vanilla = (TService) Activator.CreateInstance(proxy.CreateType(), ctorArgs.ToArray());
            }
            else
            {
                Instance.Vanilla = (TService) vanilla;
            }

            foreach(var property in typeof(TInstance).GetProperties(InstanceBinding | BindingFlags.Static))
            {
                if(property.HasAttribute<HarmonyPropertyAttribute>())
                {
                    property.InjectVanillaData(vanillaImplType, harmony);
                }
            }
        }
    }

    internal static class PatchHelpers
    {
        private static ModuleBuilder DynamicModuleCache;
        private static AssemblyBuilder DynamicAssemblyCache;
        
        private static ConcurrentDictionary<MethodBase, (MethodBase, int)> ReplaceTargets = new ConcurrentDictionary<MethodBase, (MethodBase, int)>();
        private static HarmonyMethod PatchVanillaCallTranspiler = new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => VanillaTranpolineTranspilerImpl(null, null, null)));

        private static ConcurrentDictionary<MethodInfo, MemberInfo> VanillaPropertyInjectTargets = new ConcurrentDictionary<MethodInfo, MemberInfo>();
        private static HarmonyMethod VanillaPropertyTranspiler = new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => VanillaPropertyTranspilerImpl(null, null)));

        public static void VanillaReplace(this ServicePatch patch, MethodBase creativeTarget, int stubId)
        {
            Log.Debug.PrintLine($"VanillaReplace {creativeTarget} -> {patch.Target}");

            ReplaceTargets[patch.Target] = (creativeTarget, stubId);
            patch.Patch.AddTranspiler(PatchVanillaCallTranspiler);
        }

        public static void InjectVanillaData(this PropertyInfo target, Type vanillaImpl, Harmony harmony)
        {
            var vanillaTarget = FindVanillaDataTarget(target, vanillaImpl);
            Log.Debug.PrintLine($"InjectVanillaData {vanillaTarget} -> {target}");

            VanillaPropertyInjectTargets[target.GetMethod] = vanillaTarget;
            VanillaPropertyInjectTargets[target.SetMethod] = vanillaTarget;

            harmony.Patch(target.GetMethod, transpiler: VanillaPropertyTranspiler);
            harmony.Patch(target.SetMethod, transpiler: VanillaPropertyTranspiler);
        }

        private static IEnumerable<CodeInstruction> VanillaPropertyTranspilerImpl(IEnumerable<CodeInstruction> _, MethodBase creativeTargetBase)
        {
            var creativeTarget = (MethodInfo) creativeTargetBase;
            var target = VanillaPropertyInjectTargets[creativeTarget];

            var field = target as FieldInfo;
            var property = target as PropertyInfo;

            var isGetter = creativeTarget.ReturnType != typeof(void);
            var isStatic = field?.IsStatic ?? (isGetter ? property.GetMethod.IsStatic : property.SetMethod.IsStatic);

            if(isStatic == false)
            {
                var creativeServiceType = creativeTarget.DeclaringType;
                var vanillaProperty = AccessTools.Property(creativeServiceType, "Vanilla");

                yield return new CodeInstruction(OpCodes.Ldarg_0); //This
                yield return new CodeInstruction(OpCodes.Call, vanillaProperty.GetMethod);

                var creativeServiceInstance = AccessTools.Field(creativeServiceType, "Instance").GetValue(null);
                var vanillaServiceInstance = vanillaProperty.GetValue(creativeServiceInstance);
                if (vanillaServiceInstance.GetType() != target.DeclaringType)
                {
                    //Vanilla instance is proxied
                    var proxyField = AccessTools.Field(vanillaServiceInstance.GetType(), "__Vanilla");
                    yield return new CodeInstruction(OpCodes.Ldfld, proxyField);
                }
            }

            if(isGetter)
            {
                if(field != null)
                {
                    var opCode = isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
                    yield return new CodeInstruction(opCode, field);
                }
                else
                {
                    yield return new CodeInstruction(OpCodes.Call, property.GetMethod);
                }
            }
            else
            {
                yield return new CodeInstruction(OpCodes.Ldarg_1); //value

                if (field != null)
                {
                    var opCode = isStatic ? OpCodes.Stsfld : OpCodes.Stfld;
                    yield return new CodeInstruction(opCode, field);
                }
                else
                {
                    yield return new CodeInstruction(OpCodes.Call, property.SetMethod);
                }
            }

            yield return new CodeInstruction(OpCodes.Ret);
        }

        private static MemberInfo FindVanillaDataTarget(PropertyInfo target, Type vanillaImpl)
        {
            var targetType = target.PropertyType;
            var targetName = target.GetCustomAttribute<HarmonyPropertyAttribute>().Target ?? target.Name;

            List<MemberInfo> strongMatches = null, weakMatches = null;
            foreach (var member in vanillaImpl.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if(member is FieldInfo field)
                {
                    ProcessMember(field.FieldType, field.Name);
                }

                if(member is PropertyInfo property)
                {
                    ProcessMember(property.PropertyType, property.Name);
                }

                void ProcessMember(Type type, string name)
                {
                    if(type != targetType)
                        return;

                    if(name == targetName)
                    {
                        Add(ref strongMatches);
                    }
                    else if (name.Equals(targetName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Add(ref weakMatches);
                    }

                }

                void Add(ref List<MemberInfo> list)
                {
                    if(list == null)
                    {
                        list = new List<MemberInfo>();
                    }

                    list.Add(member);
                }
            }

            if(strongMatches?.Count == 1)
            {
                return strongMatches[0];
            }

            if(strongMatches?.Count > 1)
            {
                throw new Exception($"Ambiguous strong matches for property target {target.Name}");
            }

            if(weakMatches?.Count == 1)
            {
                return weakMatches[0];
            }

            if (strongMatches?.Count > 1)
            {
                throw new Exception($"Ambiguous weak matches for property target {target.Name}");
            }

            throw new Exception($"No match for property target {target.Name}");
        }

        public static MethodInfo FindMatchingMethod(this MethodInfo method, IEnumerable<MethodInfo> candidates, bool throwOnFail, Func<Type, Type, bool> IsTypeMatching = null)
        {
            if(IsTypeMatching == null)
            {
                IsTypeMatching = (a, b) => a == b;
            }

            var methodParams = method.GetParameters();
            candidates = candidates.Where(x => x.Name == method.Name)
                .Where(x => IsTypeMatching(method.ReturnType, x.ReturnType))
                .Where(x =>
                {
                    var xParams = x.GetParameters();
                    if(xParams.Length != methodParams.Length)
                        return false;

                    for(int i = 0; i < xParams.Length; i++)
                    {
                        if(IsTypeMatching(xParams[i].ParameterType, methodParams[i].ParameterType) == false)
                        {
                            return false;
                        }
                    }

                    return true;
                });

            using(var enumerator = candidates.GetEnumerator())
            {
                if(enumerator.MoveNext() == false)
                {
                    if(throwOnFail)
                    {
                        throw new Exception("No match for " + method);
                    }

                    return null;
                }

                var match = enumerator.Current;

                if(enumerator.MoveNext())
                {
                    var l = Log.Error;
                    l.PrintLine("Ambiguous match for " + method);
                    l.PrintLine("Matches:");
                    using(l.Indent())
                    {
                        foreach(var c in candidates)
                        {
                            l.PrintLine(c.ToString());
                        }
                    }

                    throw new AmbiguousMatchException("Ambiguous match for " + method);
                }

                return match;
            }
        }

        private static IEnumerable<CodeInstruction> VanillaTranpolineTranspilerImpl(IEnumerable<CodeInstruction> originalInstructions, ILGenerator IL, MethodBase originalMethod)
        {
            var (creativeTarget, stubId) = ReplaceTargets[originalMethod];
            var creativeType = creativeTarget.DeclaringType;
            var stubs = AccessTools.Field(creativeType, "StubIndirections");

            var instructions = new List<CodeInstruction>();
            if(stubId < 0)
            {
                //No stub means that this is pure replacement
                //Vanilla code is not accessible so we can drop it
                originalInstructions = Array.Empty<CodeInstruction>();
            }
            else
            {
                //Read and...
                instructions.Add(new CodeInstruction(OpCodes.Ldsfld, stubs));
                instructions.Add(new CodeInstruction(OpCodes.Ldc_I4, stubId));
                instructions.Add(new CodeInstruction(OpCodes.Ldelem_U1));

                //...reset stub branch indicator
                instructions.Add(new CodeInstruction(OpCodes.Ldsfld, stubs));
                instructions.Add(new CodeInstruction(OpCodes.Ldc_I4, stubId));
                instructions.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                instructions.Add(new CodeInstruction(OpCodes.Stelem_I1));

                //Resolve branch
                var firstVanillaInstructionLabel = IL.DefineLabel();
                originalInstructions.First().labels.Add(firstVanillaInstructionLabel);
                instructions.Add(new CodeInstruction(OpCodes.Brtrue, firstVanillaInstructionLabel));
            }

            //Emit creative call
            var argOffset = 0;
            if(creativeTarget.IsStatic == false)
            {
                argOffset = 1;
                instructions.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(creativeType, "Instance")));
            }

            var argCount = creativeTarget.GetParameters().Length;
            for(int i = 0; i < argCount; i++)
            {
                instructions.Add(new CodeInstruction(OpCodes.Ldarg, argOffset + i));
            }

            instructions.Add(new CodeInstruction(OpCodes.Call, creativeTarget));
            instructions.Add(new CodeInstruction(OpCodes.Ret));

            return instructions.Concat(originalInstructions);
        }

        public static TypeBuilder CreateDynamicType(string name)
        {
            if(DynamicAssemblyCache == null)
            {
                DynamicAssemblyCache = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("CreativeZone.Dynamic"), AssemblyBuilderAccess.Run);
                DynamicModuleCache = DynamicAssemblyCache.DefineDynamicModule("CreativeZone.Dynamic.Module");
            }

            return DynamicModuleCache.DefineType(name);
        }

        public static bool Implements(this Type type, Type @interface)
        {
            return type.Interfaces().Contains(@interface);
        }

        public static IEnumerable<MethodInfo> GetAllInterfaceMethods(this Type @interface)
        {
            var typesToVisit = new Stack<Type>();
            var visitedTypes = new HashSet<Type>();

            visitedTypes.Add(@interface);
            typesToVisit.Push(@interface);
            while(typesToVisit.Count > 0)
            {
                var current = typesToVisit.Pop();
                foreach(var method in current.GetMethods())
                {
                    yield return method;
                }

                foreach(var next in current.GetInterfaces())
                {
                    if(visitedTypes.Add(next))
                    {
                        typesToVisit.Push(next);
                    }
                }
            }
        }

        public static bool IsAssemblyPublic(this MethodBase target)
        {
            if(target.IsPrivate)
                return false;

            var type = target.DeclaringType;
            while(type != null)
            {
                if(type.IsNotPublic)
                    return false;

                type = type.DeclaringType;
            }

            return true;
        }
    }
}
