namespace LoxScript.VirtualMachine {
    /// <summary>
    /// An object is a heap-allocated object. It can represent a string, function, class, etc.
    /// </summary>
    class GearsObj {
        public ObjType Type;

        public enum ObjType {
            ObjFunction,
            ObjNative,
            ObjString,
        }

        public override string ToString() => "GearsObj";
    }

    /// <summary>
    /// A function is a first class variable, and so must be an object.
    /// </summary>
    class GearsObjFunction : GearsObj {
        public readonly string Name;

        /// <summary>
        /// The number of parameters expected by the function.
        /// </summary>
        public int Arity;
        public readonly GearsChunk Chunk;

        public GearsObjFunction(string name, int arity) {
            Type = ObjType.ObjFunction;
            Name = name;
            Arity = arity;
            Chunk = new GearsChunk(name);
        }

        public GearsObjFunction(string name, int arity, GearsChunk chunk) {
            Name = name;
            Arity = arity;
            Chunk = chunk;
        }

        /// <summary>
        /// Deserialize from a context.
        /// </summary>
        public GearsObjFunction(GearsContext context) {
            Type = ObjType.ObjFunction;
            int index = context.ReadShort();
            Name = context.Frame.Function.Chunk.ReadConstantString(ref index);
            Arity = context.Frame.Function.Chunk.ReadConstantByte(ref index);
            Chunk = new GearsChunk(Name,
                context.Frame.Function.Chunk.ReadConstantBytes(ref index),
                context.Frame.Function.Chunk.ReadConstantBytes(ref index));
        }

        /// <summary>
        /// Serialize to a chunk.
        /// </summary>
        internal int Serialize(GearsChunk writer) {
            int index = writer.WriteConstantString(Name);
            writer.WriteConstantByte((byte)Arity);
            Chunk.Compress();
            writer.WriteConstantShort(Chunk.CodeSize);
            if (Chunk.CodeSize > 0) {
                writer.WriteConstantBytes(Chunk._Code);
            }
            writer.WriteConstantShort(Chunk.ConstantSize);
            if (Chunk.ConstantSize > 0) {
                writer.WriteConstantBytes(Chunk._Constants);
            }
            return index;
        }

        public override string ToString() => Name == null ? "<script>" : $"<fn {Name}>";
    }

    class GearsObjNativeFunction : GearsObj {

        public GearsObjNativeFunction(string value) {
            Type = ObjType.ObjNative;
        }

        public override string ToString() => "<native>";

    }

    class GearsObjString : GearsObj {
        public readonly string Value;

        public GearsObjString(string value) {
            Type = ObjType.ObjString;
            Value = value;
        }

        public override string ToString() => Value;
    }
}
