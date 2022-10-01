namespace XPT.Core.Scripting.Rules {
    struct RuleInvocationResult {
        internal readonly string FnName;
        internal readonly bool WasSuccessfullyInvoked;
        internal readonly object ReturnedValue;

        internal RuleInvocationResult(string fnName, bool success, object returned) {
            FnName = fnName;
            WasSuccessfullyInvoked = success;
            ReturnedValue = returned;
        }
    }
}
