using System;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// An object is a heap-allocated object. It can represent a string, function, class, etc.
    /// </summary>
    class GearsObj {
        public ObjType Type;

        public enum ObjType {
            ObjClosure,
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
        /// Deserialize from a chunk's constant storage...
        /// ... will be moed to code later.
        /// </summary>
        public GearsObjFunction(GearsContext context) {
            Type = ObjType.ObjFunction;
            int index = context.ReadShort();
            Name = context.Chunk.ReadConstantString(ref index);
            Arity = context.Chunk.ReadConstantByte(ref index);
            Chunk = new GearsChunk(Name,
                context.Chunk.ReadConstantBytes(ref index),
                context.Chunk.ReadConstantBytes(ref index));
        }

        /// <summary>
        /// Serialize to a chunk's constant storage...
        /// ... will be moved to code later.
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

    delegate GearsValue GearsNativeFunction(GearsValue[] args);

    class GearsObjNativeFunction : GearsObj {
        public readonly string Name;

        /// <summary>
        /// The number of parameters expected by the function.
        /// </summary>
        public int Arity;

        private GearsNativeFunction _OnInvoke;

        public GearsObjNativeFunction(string name, int arity, GearsNativeFunction onInvoke) {
            Type = ObjType.ObjNative;
            Name = name;
            Arity = arity;
            _OnInvoke = onInvoke;
        }

        public GearsValue Invoke(params GearsValue[] args) {
            return _OnInvoke(args);
        }

        public override string ToString() => $"<native {Name}>";
    }

    class GearsObjString : GearsObj {
        public readonly string Value;

        public GearsObjString(string value) {
            Type = ObjType.ObjString;
            Value = value;
        }

        public override string ToString() => Value;
    }

    class GearsObjClosure : GearsObj {
        public readonly GearsObjFunction Function;

        public GearsObjClosure(GearsObjFunction fn) {
            Type = ObjType.ObjClosure;
            Function = fn;
        }
    }
}
