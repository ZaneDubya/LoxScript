namespace XPT.Core.Scripting.Rules {
    internal delegate void RuleInvocationDelegateNative(ValueCollection vars); // use this for c# code

    internal delegate void RuleInvocationDelegateHosted(string fnName, ValueCollection vars); // use this for loxscript code
}
