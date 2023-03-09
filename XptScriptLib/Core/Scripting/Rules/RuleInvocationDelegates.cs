namespace XPT.Core.Scripting.Rules {

    /// <summary>
    /// A method that can be invoked from C# code.
    /// </summary>
    internal delegate void RuleInvocationDelegateNative(VarCollection vars);

    /// <summary>
    /// A method that can be invoked from LoxScript code.
    /// </summary>
    internal delegate void RuleInvocationDelegateHosted(string fnName, VarCollection vars);
}
