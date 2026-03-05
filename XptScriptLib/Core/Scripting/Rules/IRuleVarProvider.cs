namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// Interface for objects that can provide variable values to the rule system.
    /// </summary>
    internal interface IRuleVarProvider {
        /// <summary>
        /// Tries to get the value of a variable by key.
        /// </summary>
        /// <param name="key">The variable key (case-insensitive).</param>
        /// <param name="value">The variable value if found.</param>
        /// <returns>True if the variable was found; otherwise false.</returns>
        bool TryGetRuleVar(string key, out object value);
    }
}
