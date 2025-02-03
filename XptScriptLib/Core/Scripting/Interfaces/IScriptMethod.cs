using System.Collections.Generic;

namespace XPT.Core.Scripting.Interfaces {
    internal interface IScriptMethod {
        string Name { get; set; }
        IEnumerable<string> Parameters { get; }
        string Code { get; set; }

        /// <summary>
        /// Returns a complete source code representation of the script method, ready to be passed to a compiler.
        /// </summary>
        /// <returns></returns>
        string GenerateSource();

        /// <summary>
        /// Should be false for any special methods created by the editor that must have specific parameters.
        /// </summary>
        bool ParameterEdittingEnabled { get; }

        void ParameterAdd(string parameter);
        void ParameterRemove(string parameter);
        void ParameterReorder(string parameter, int order);
    }
}
