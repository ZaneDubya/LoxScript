namespace XPT.Core.Scripting.Base {
    internal static class Validation {
        internal static bool IsAlphaOrUnderscore(char c) {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
        }

        internal static bool IsAlphaUnderscoreOrNumeric(char c) {
            return IsAlphaOrUnderscore(c) || IsDigit(c);
        }

        internal static bool IsDigit(char c, bool allowHex = false) {
            return (c >= '0' && c <= '9') || (allowHex && ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')));
        }

        internal static bool IsValidIdentifer(string s, out string error, bool allowUnderscoreFirstCharacter = true) {
            error = null;
            if (string.IsNullOrWhiteSpace(s)) {
                error = "Identifier must not be null or contain whitespace.";
                return false;
            }
            if (!allowUnderscoreFirstCharacter && s[0] == '_') {
                error = "Identifier must not begin with an underscore character.";
                return false;
            }
            if (!IsAlphaOrUnderscore(s[0])) {
                error = "Identifier must begin with an alphabetical or underscore character.";
                return false;
            }
            for (int i = 1; i < s.Length; i++) {
                char c = s[i];
                if (!IsAlphaOrUnderscore(c) && !IsDigit(c)) {
                    error = "Identifier must only contain alphanumeric characters and underscore characters.";
                    return false;
                }
            }
            return true;
        }
    }
}
