using XPT.Core.Scripting.Rules;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// Information about a top-level function in a LoxScript, including its name, parameter count, and any attached rules.
    /// </summary>
    internal class GearsFunctionInfo {
        /// <summary>
        /// The name of the function as it appears in the global scope.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// The number of parameters the function accepts. Parameter names are not preserved in bytecode.
        /// </summary>
        internal int Arity { get; }

        /// <summary>
        /// Array of rules attached to this function. Rules are triggered by events and invoke this function when conditions are met.
        /// </summary>
        internal Rule[] Rules { get; }

        internal GearsFunctionInfo(string name, int arity, Rule[] rules) {
            Name = name;
            Arity = arity;
            Rules = rules ?? new Rule[0];
        }

        public override string ToString() {
            string rulesStr = Rules.Length > 0 ? $", {Rules.Length} rule(s)" : "";
            return $"{Name}({Arity} param(s){rulesStr})";
        }
    }
}
