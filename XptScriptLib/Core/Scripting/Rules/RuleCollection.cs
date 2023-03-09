using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class RuleCollection {
        private readonly static Type TypeOfVoid = typeof(void);
        private readonly static Type TypeOfValueCollection = typeof(VarCollection);
        private readonly static Type TypeOfDelegateNative = typeof(RuleInvocationDelegateNative);
        private readonly static BindingFlags Binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // === instance ==============================================================================================
        // ===========================================================================================================

        public int Count => _Rules?.Count ?? 0;

        private List<Rule> _Rules;

        /// <summary>
        /// Creates invocable rules for every native c# method with a RuleAttribute.
        /// </summary>
        internal void AddNativeFromObject(object obj) {
            Type type = obj.GetType();
            MethodInfo[] methods = type.GetMethods(Binding).Where(d => !d.IsSpecialName).ToArray();
            foreach (MethodInfo method in methods) {
                RuleAttribute attr = method.GetCustomAttribute<RuleAttribute>();
                if (attr == null) {
                    continue;
                }
                if (method.ReturnType != TypeOfVoid) {
                    throw new Exception($"RuleCollection: Method {type.Name}.{method.Name}() must be return void.");
                }
                ParameterInfo[] ps = method.GetParameters();
                if (ps.Length != 1 || ps[0].ParameterType != TypeOfValueCollection) {
                    throw new Exception($"RuleCollection: Method {type.Name}.{method.Name}() must accept one ValueCollection parameter.");
                }
                string trigger = attr.Trigger;
                string fnName = method.Name;
                RuleCondition[] conditions = attr.Conditions;
                RuleInvocationDelegateNative fn = (RuleInvocationDelegateNative)Delegate.CreateDelegate(TypeOfDelegateNative, obj, method);
                AddRule(new Rule(trigger, fnName, conditions, fn));
            }
        }

        internal void AddRule(Rule rule) {
            if (_Rules == null) {
                _Rules = new List<Rule>();
            }
            _Rules.Add(rule);
        }

        /// <summary>
        /// Returns all the rules that match the passed trigger and context.
        /// </summary>
        internal IEnumerable<Rule> GetMatching(string triggerName, VarCollection context) {
            if (_Rules == null) {
                yield break;
            }
            foreach (Rule rule in _Rules) {
                if (rule.Match(triggerName, context)) {
                    yield return rule;
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of this Collection, 
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        internal RuleCollection CreateCopyWithHostedDelegate(RuleInvocationDelegateHosted fn) {
            if (_Rules == null) {
                return null;
            }
            RuleCollection collection = new RuleCollection();
            foreach (Rule rule in _Rules) {
                collection.AddRule(rule.CreateCopyWithHostedDelegate(fn));
            }
            return collection;
        }

        // === Serialization / Deserialization =======================================================================
        // ===========================================================================================================

        internal void Serialize(IWriter writer) {
            writer.WriteFourBytes("rulx");
            writer.Write7BitInt(_Rules?.Count ?? 0);
            for (int i = 0; i < _Rules?.Count; i++) {
                _Rules[i].Serialize(writer);
            }
        }

        internal static bool TryDeserialize(IReader reader, out RuleCollection collection) {
            if (!reader.ReadFourBytes("rulx")) {
                collection = null;
                return false;
            }
            collection = new RuleCollection();
            int count = reader.Read7BitInt();
            if (count == 0) {
                return true;
            }
            for (int i = 0; i < count; i++) {
                Rule rule = Rule.Deserialize(reader);
                collection.AddRule(rule);
            }
            return true;
        }
    }
}
