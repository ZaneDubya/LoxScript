using System.Collections.Generic;

namespace XPT.Core.Scripting.Interfaces {
    internal interface IScript {

        /// <summary>
        /// The name of the script.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The global variables and other data stored in the global string.
        /// </summary>
        string ScriptGlobals { get; set; }

        /// <summary>
        /// A collection of methods, each corresponding to a function in the script.
        /// </summary>
        IEnumerable<IScriptMethod> ScriptMethods { get; }

        /// <summary>
        /// The entire text source code for this script.
        /// </summary>
        string ScriptSource { get; }

        bool TryAddMethod(string name, out string error);
        bool TryGetMethod(string name, out IScriptMethod method);
        bool TryRemoveMethod(string name);
        int GetMethodUseCount(string name);
    }
}
