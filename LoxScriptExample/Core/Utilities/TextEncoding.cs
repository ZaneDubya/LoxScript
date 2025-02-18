using System;
using System.Collections.Generic;
using System.Text;

namespace XPT.Core.Utilities {
    /// <summary>
    /// A custom text encoding page.
    /// </summary>
    internal static class TextEncoding {

        // this is an array of characters that represent code page 1252:
        private static readonly char[] CodePage1252 = {
            '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\t', '\n', '\x0', '\x0', '\x0', '\x0', '\x0',
            '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0',
            ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', 
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
            '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
            'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\',']', '^', '_',
            '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
            'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z','{', '|', '}', '~', '\x0',
            '\x20AC', '\x0', '\x201A', '\x0192', '\x201E', '\x2026', '\x2020', '\x2021', '\x02C6', '\x2030', '\x0160', '\x2039', '\x0152', '\x0', '\x017D', '\x0',
            '\x0', '\x2018', '\x2019', '\x201C', '\x201D', '\x2022', '\x2013', '\x2014', '\x02DC', '\x2122', '\x0161', '\x203A', '\x0153', '\x0', '\x017E', '\x0178',
            '\x0', '\xA1', '\xA2', '\xA3', '\xA4', '\xA5', '\xA6', '\xA7', '\xA8', '\xA9', '\xAA', '\xAB', '\xAC', '\xAD', '\xAE', '\xAF',
            '\xB0', '\xB1', '\xB2', '\xB3', '\xB4', '\xB5', '\xB6', '\xB7', '\xB8', '\xB9', '\xBA', '\xBB', '\xBC', '\xBD', '\xBE', '\xBF',
            '\xC0', '\xC1', '\xC2', '\xC3', '\xC4', '\xC5', '\xC6', '\xC7', '\xC8', '\xC9', '\xCA', '\xCB', '\xCC', '\xCD', '\xCE', '\xCF',
            '\xD0', '\xD1', '\xD2', '\xD3', '\xD4', '\xD5', '\xD6', '\xD7', '\xD8', '\xD9', '\xDA', '\xDB', '\xDC', '\xDD', '\xDE', '\xDF',
            '\xE0', '\xE1', '\xE2', '\xE3', '\xE4', '\xE5', '\xE6', '\xE7', '\xE8', '\xE9', '\xEA', '\xEB', '\xEC', '\xED', '\xEE', '\xEF',
            '\xF0', '\xF1', '\xF2', '\xF3', '\xF4', '\xF5', '\xF6', '\xF7', '\xF8', '\xF9', '\xFA', '\xFB', '\xFC', '\xFD', '\xFE', '\xFF'
        };

        private static bool _IsInitialized = false;
        private static readonly StringBuilder _StringBuilder = new StringBuilder(1024);
        private static char[] _StringBuilding = new char[1024];

        private static readonly Dictionary<char, byte> CodePage1252Lookup = new Dictionary<char, byte>();

        public static void Initialize() {
            if (_IsInitialized) {
                return;
            }
            _IsInitialized = true;
            // initialize the code page lookup table:
            for (int i = 0; i < CodePage1252.Length; i++) {
                if (CodePage1252[i] == '\x0') {
                    continue;
                }
                CodePage1252Lookup.Add(CodePage1252[i], (byte)i);
            }
        }

        // === GetBytes ==============================================================================================
        // ===========================================================================================================

        internal static byte[] GetBytes(string chars) {
            if (!_IsInitialized) {
                Initialize();
            }
            SharedBuffer buffer = new SharedBuffer("TextEncoding", chars.Length, true);
            int count = 0;
            for (int i = 0; i < chars.Length; i++) {
                if (!CodePage1252Lookup.TryGetValue(chars[i], out byte value)) {
                    continue;
                }
                buffer[count] = value;
                count += 1;
            }
            byte[] data = new byte[count];
            Array.Copy(buffer.Buffer, data, count);
            buffer.Dispose();
            return data;
        }

        internal static int GetBytes(string chars, int charIndex, int charCount, byte[] buffer, int bufferIndex) {
            if (!_IsInitialized) {
                Initialize();
            }
            if (chars == null || buffer == null) {
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"));
            }
            if (charIndex < 0 || charCount < 0) {
                throw new ArgumentOutOfRangeException((charIndex < 0 ? "charIndex" : "charCount"));
            }
            if (chars.Length - charIndex < charCount) {
                throw new ArgumentOutOfRangeException("chars");
            }
            if (bufferIndex < 0 || bufferIndex > buffer.Length) {
                throw new ArgumentOutOfRangeException("byteIndex");
            }
            int count = 0;
            for (int i = charIndex; i < charIndex + charCount; i++) {
                if (!CodePage1252Lookup.TryGetValue(chars[i], out byte value)) {
                    continue;
                }
                buffer[bufferIndex + count] = value;
                count += 1;
            }
            return count;
        }

        // === GetString =============================================================================================
        // ===========================================================================================================

        internal static string GetString(byte[] data) => GetString(data, 0, data.Length);

        internal static string GetString(byte[] data, int index, int count) {
            if (!_IsInitialized) {
                Initialize();
            }
            // Validate Parameters
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            if (index < 0 || count < 0) {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count));
            }
            if (data.Length - index < count) {
                throw new ArgumentOutOfRangeException(nameof(data));
            }
            // Avoid problems with empty input buffer
            if (count == 0) {
                return string.Empty;
            }
            while (_StringBuilding.Length < count) {
                count *= 2;
                _StringBuilding = new char[count];
            }
            int resultIndex = 0;
            for (int i = index; i < index + count; i++) {
                char character = CodePage1252[data[i]];
                if (character != '\x0') { // Skip invalid characters
                    _StringBuilding[resultIndex++] = character;
                }
            }
            // Create the string using the valid portion of the array
            return new string(_StringBuilding, 0, resultIndex);
        }

        internal static string GetString2(byte[] data, int index, int count) {
            if (!_IsInitialized) {
                Initialize();
            }
            // Validate Parameters
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (index < 0 || count < 0) {
                throw new ArgumentOutOfRangeException((index < 0 ? "byteIndex" : "byteCount"));
            }
            if (data.Length - index < count) {
                throw new ArgumentOutOfRangeException("data");
            }
            // Avoid problems with empty input buffer
            if (data.Length == 0) {
                return string.Empty;
            }
            _StringBuilder.Clear();
            for (int i = index; i < index + count; i++) {
                if (CodePage1252[data[i]] == '\x0') {
                    continue;
                }
                _StringBuilder.Append((char)data[i]);
            }
            return _StringBuilder.ToString();
        }
    }
}
