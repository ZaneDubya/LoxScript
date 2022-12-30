﻿using System.Collections.Generic;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class RuleCollection {
        private readonly List<Rule> _Rules = new List<Rule>();

        internal void AddRule(Rule rule) {
            _Rules.Add(rule);
        }

        /// <summary>
        /// Returns the "ResultFnName" value of all rules that match the passed trigger and context.
        /// </summary>
        internal IEnumerable<string> AttemptMatch(string trigger, RuleInvocationContext context) {
            foreach (Rule rule in _Rules) {
                if (rule.IsTrue(trigger, context)) {
                    yield return rule.ResultFnName;
                }
            }
        }

        internal void Serialize(IWriter writer) {
            writer.WriteFourBytes("rulx");
            writer.Write7BitInt(_Rules.Count);
            for (int i = 0; i < _Rules.Count; i++) {
                _Rules[i].Serialize(writer);
            }
        }

        internal static bool Deserialize(IReader reader, out RuleCollection collection) {
            if (!reader.ReadFourBytes("rulx")) {
                collection = null;
                return false;
            }
            collection = new RuleCollection();
            int count = reader.Read7BitInt();
            for (int i = 0; i < count; i++) {
                Rule rule = Rule.Deserialize(reader);
                collection.AddRule(rule);
            }
            return true;
        }
    }
}