using System.Collections.Generic;

namespace XPT.Core.Scripting.LoxScript.Compiling {
    internal static class LoxTokenTypes {
        // Keywords.                                     
        internal const int
            AND = 100, CLASS = 101, ELSE = 102, FALSE = 103,
            FUNCTION = 104, FOR = 105, IF = 106, NIL = 107,
            OR = 108, /* PRINT = 109, */ RETURN = 110, SUPER = 111,
            THIS = 112, TRUE = 113, VAR = 114, WHILE = 115 /*, PUBLIC = 116 */ ;

        internal const string This = "this";
        internal const string Ctor = "init";

        private static readonly Dictionary<string, int> _Keywords = new Dictionary<string, int>();

        static LoxTokenTypes() {
            _Keywords["and"] = AND;
            _Keywords["class"] = CLASS;
            _Keywords["else"] = ELSE;
            _Keywords["false"] = FALSE;
            _Keywords["for"] = FOR;
            _Keywords["fun"] = FUNCTION;
            _Keywords["if"] = IF;
            _Keywords["nil"] = NIL;
            _Keywords["or"] = OR;
            // _Keywords["public"] = PUBLIC;
            _Keywords["return"] = RETURN;
            _Keywords["super"] = SUPER;
            _Keywords["this"] = THIS;
            _Keywords["true"] = TRUE;
            _Keywords["var"] = VAR;
            _Keywords["while"] = WHILE;
        }

        internal static int? Get(string text) {
            if (_Keywords.TryGetValue(text, out int type)) {
                return type;
            }
            return null;
        }
    }
}
