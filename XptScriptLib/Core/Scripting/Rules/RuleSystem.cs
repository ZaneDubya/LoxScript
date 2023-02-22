using System;
using System.Collections.Generic;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// RuleSystem hosts all the active RuleCollections, with Rules composed of a named Trigger and set of Conditions.
    /// In-game events are composed of a named Trigger and a a Context of all variables accessible by Rules.
    /// RuleSystem will then compare the event Trigger/Context against each Rule's Trigger/Conditions.
    /// When there is a match, the function associated with the Rule will be invoked.
    /// </summary>
    internal static class RuleSystem {

        private static readonly Dictionary<object, RuleCollection> _RuleCollections = new Dictionary<object, RuleCollection>();

        /// <summary>
        /// Load a single rule into RuleSystem. Parameter key is the object handling invocations of this rule.
        /// When the handling object is disposed, you must also call UnloadRules.
        /// </summary>
        internal static void RegisterRule(object key, Rule rule) {
            if (!_RuleCollections.TryGetValue(key, out RuleCollection collection)) {
                collection = new RuleCollection();
                _RuleCollections[key] = collection;
            }
            collection.AddRule(rule);
        }

        /// <summary>
        /// Load a rule collection into RuleSystem. Parameter key is the object handling invocations of these rule.
        /// When the handling object is disposed, you must also call UnloadRules.
        /// </summary>
        internal static void RegisterRules(object key, RuleCollection rules) {
            _RuleCollections[key] = rules;
        }

        /// <summary>
        /// Create a rule collection for RuleSystem. Parameter key is the object handling invocations of these rule.
        /// When the handling object is disposed, you must also call UnloadRules.
        /// </summary>
        internal static void RegisterRulesFromNativeObject(object key) {
            RuleCollection rules = new RuleCollection();
            rules.AddNativeFromObject(key);
            _RuleCollections[key] = rules;
        }

        /// <summary>
        /// Unloads the RuleCollection, or rules, associated with the parameter key.
        /// </summary>
        internal static void UnregisterRules(object key) {
            _RuleCollections.Remove(key);
        }

        internal static void InvokeTrigger(string triggerName, ValueCollection vars) {
            foreach (RuleCollection rules in _RuleCollections.Values) {
                foreach (Rule rule in rules.GetMatching(triggerName, vars)) {
                    rule.Invoke(vars);
                }
            }
        }
    }
}
