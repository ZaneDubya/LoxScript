using System.Collections.Generic;

namespace XPT.Core.Scripting.Interfaces {
    internal interface IScript {
        string Name { get; }
        string ScriptGlobals { get; set; }
        IEnumerable<IScriptMethod> ScriptMethods { get; }
        bool TryAddMethod(string name, out string error);
        bool TryGetMethod(string name, out IScriptMethod method);
        bool TryRemoveMethod(string name);
        int GetMethodUseCount(string name);
    }
}
