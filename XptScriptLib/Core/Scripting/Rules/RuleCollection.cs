using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// RuleCollection is a collection of rules that can be invoked by a trigger. You will have one RuleCollection
    /// per script or object.
    /// </summary>
    class RuleCollection {

        // === instance ==============================================================================================
        // ===========================================================================================================

        public int Count => _Rules?.Count ?? 0;

        private List<Rule> _Rules;

        internal void AddRule(Rule rule) {
            if (_Rules == null) {
                _Rules = new List<Rule>();
            }
            _Rules.Add(rule);
        }

        /// <summary>
        /// Returns all the rules that match the passed trigger and context.
        /// </summary>
        internal IEnumerable<Rule> GetMatching(string triggerName, RuleVarCollection vars) {
            if (_Rules == null) {
                yield break;
            }
            foreach (Rule rule in _Rules) {
                if (rule.Match(triggerName, vars)) {
                    yield return rule;
                }
            }
        }

        /// <summary>
        /// Returns all rules in the collection.
        /// </summary>
        internal IEnumerable<Rule> GetAll() {
            if (_Rules == null) {
                yield break;
            }
            foreach (Rule rule in _Rules) {
                yield return rule;
            }
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
