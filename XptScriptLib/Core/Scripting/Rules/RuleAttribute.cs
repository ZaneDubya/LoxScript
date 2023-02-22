using System;
using XPT.Core.Scripting.Rules.Compiling;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// A 'Rule' indicates that this method can be invoked by RuleSystem.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class RuleAttribute : Attribute {

        // === Instance ==============================================================================================
        // ===========================================================================================================

        internal readonly string Trigger;
        internal readonly RuleCondition[] Conditions;

        internal RuleAttribute(string definition) {
            if (!RuleCompiler.TryCompile(definition, out string trigger, out RuleCondition[] conditions)) {
                throw new Exception($"Failed to compile Rule with definition '{definition}'");
            }
            Trigger = trigger;
            Conditions = conditions;
        }
    }
}
