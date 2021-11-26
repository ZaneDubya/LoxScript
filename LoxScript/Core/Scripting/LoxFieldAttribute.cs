using System;

namespace XPT.Core.Scripting {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class LoxFieldAttribute : Attribute {
        public readonly string Name;

        public LoxFieldAttribute(string name) {
            Name = name;
        }
    }
}
