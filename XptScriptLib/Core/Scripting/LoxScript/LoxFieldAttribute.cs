using System;

namespace XPT.Core.Scripting.LoxScript {
    /// <summary>
    /// Attach this to a field, property, or method to make it callable by a lox script running in a gears virtual machine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class LoxFieldAttribute : Attribute {
        public readonly string Name;

        public LoxFieldAttribute(string name) {
            Name = name;
        }
    }
}
