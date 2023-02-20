using System;
using XPT.Core.Scripting.Rules.Compiling;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// A 'Rule' indicates that this method can be invoked by RuleSystem.
    /// It should be accompanied by multiple RuleCondition attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class RuleAttribute : Attribute {
        internal readonly string Trigger;
        internal readonly RuleCondition[] Conditions;

        internal RuleAttribute(string definition) {
            if (!SingleRuleCompiler.TryCompile(definition, out Trigger, out Conditions)) {
                throw new Exception($"Failed to compile Rule with definition '{definition}'");
            }
        }
    }
}
