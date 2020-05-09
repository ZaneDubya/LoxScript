using System;

namespace LoxScript.Compiling {
    class CompilerBitStr {
        // === String speedup ========================================================================================
        // ===========================================================================================================

        /// <summary>
        /// Encodes up to the first ten characters of a string as 6-bit representations in a 64 bit ulong.
        /// </summary>
        public static ulong GetBitStr(string value) {
            ulong bits = 0;
            int bitPosition = 0;
            for (int i = 0; i < value.Length; i++) {
                char ch = value[i];
                 if (ch >= '0' && ch <= '9') {
                    // encode as binary (000001 - 001010) (1-10)
                    ulong bitValue = (ulong)(ch - '0') + 0b000001;
                    bits |= (bitValue << bitPosition);
                }
                else if (ch >= 'A' && ch <= 'Z') {
                    // encode as 001011 - 100100 (11-36)
                    ulong bitValue = (ulong)(ch - 'A') + 0b001011;
                    bits |= (bitValue << bitPosition);
                }
                else if (ch >= 'a' && ch <= 'z') {
                    // encode as 100101‬ - 111110 (37-62)
                    ulong bitValue = (ulong)(ch - 'a') + 0b100101;
                    bits |= (bitValue << bitPosition);
                }
                else if (ch == '_') {
                    // encode as 111111 (63)
                    ulong bitValue = 0b111111;
                    bits |= (bitValue << bitPosition);
                }
                else {
                    throw new Exception($"Cannot use character '{ch}' in string '{value}'.");
                }
                bitPosition += 6;
            }
            return bits;
        }

        public static string GetBitStr(ulong value) {
            int bitPosition = 0;
            char[] chars = new char[10];
            for (int i = 0; i < 10; i++) {
                ulong bits = (value >> bitPosition) & 0x3f;
                if (bits == 0) {
                    break;
                }
                else if (bits <= 10) {
                    chars[i] = (char)('0' + bits - 1);
                }
                else if (bits <= 36) {
                    chars[i] = (char)('A' + bits - 11);
                }
                else if (bits <= 62) {
                    chars[i] = (char)('a' + bits - 37);
                }
                else if (bits == 63) {
                    chars[i] = '_';
                }
                bitPosition += 6;
            }
            return new string(chars);
        }
    }
}
