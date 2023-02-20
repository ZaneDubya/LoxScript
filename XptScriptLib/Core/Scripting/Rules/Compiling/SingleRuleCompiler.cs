namespace XPT.Core.Scripting.Rules.Compiling {
    internal static class SingleRuleCompiler {
        internal static bool TryCompile(string definition, out string trigger, out RuleCondition[] conditions) {
            trigger = null;
            conditions = null;
            return false;
        }
    }
}
