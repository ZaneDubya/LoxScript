using System;
using System.IO;
using XPT.Core.IO;
using XPT.Core.Scripting.LoxScript.Compiling;
using XPT.Core.Scripting.LoxScript.VirtualMachine;

namespace XPT.Core.Scripting.LoxScript {
    static class LoxHandler {

        internal static bool DebugTestSerialization = false;

        /// <summary>
        /// Attempt to load a file and compile it.
        /// firstrun must be true to initialize classes and declare all globals.
        /// </summary>
        internal static bool TryLoadFromFile(string path, bool firstrun, out Gears gears, out string status) {
            gears = null;
            if (!LoxCompiler.TryCompileFromPath(path, out GearsChunk chunk, out status)) {
                return false;
            }
            return TryLoad(path, chunk, firstrun, ref gears, ref status);
        }

        /// <summary>
        /// Attempt to compile passed source string.
        /// firstrun must be true to initialize classes and declare all globals.
        /// </summary>
        internal static bool TryLoadFromSource(string path, string source, bool firstrun, out Gears gears, out string status) {
            gears = null;
            if (!LoxCompiler.TryCompileFromSource(path, source, out GearsChunk chunk, out status)) {
                return false;
            }
            return TryLoad(path, chunk, firstrun, ref gears, ref status);
        }

        /// <summary>
        /// Underlying - sets up a gears machine for this chunk.
        /// </summary>
        private static bool TryLoad(string path, GearsChunk chunk, bool firstrun, ref Gears gears, ref string status) {
            if (DebugTestSerialization && !TestSerializeDeserialize(chunk, path)) {
                status = $"Error serializing GearsChunk in LoxHandler.TryLoad: Could not serialize or deserialize '{path}'.";
                return false;
            }
            try {
                gears = new Gears().Reset(chunk, firstrun);
                return true;
            }
            catch (Exception e) {
                status = $"Error initializing Gears in {e.TargetSite.DeclaringType.Name}.{e.TargetSite.Name}: {path} {e}";
                return false;
            }
        }

        private static bool TestSerializeDeserialize(GearsChunk chunk, string path) {
            string filename = $"{Path.GetFileNameWithoutExtension(path)}.lxx";
            using (BinaryFileWriter writer = new BinaryFileWriter(filename)) {
                chunk.Serialize(writer);
                writer.Close();
            }
            using (BinaryFileReader reader = new BinaryFileReader(filename)) {
                bool success = GearsChunk.TryDeserialize(filename, reader, out chunk);
                reader.Close();
                return success;
            }
        }
    }
}