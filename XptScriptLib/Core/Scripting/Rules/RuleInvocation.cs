namespace XPT.Core.Scripting.Rules {
    internal delegate object RuleInvocationWithoutName(params object[] args);

    internal delegate object RuleInvocationWithName(string fnName, params object[] args);
}
