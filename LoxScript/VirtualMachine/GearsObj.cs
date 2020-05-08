namespace LoxScript.VirtualMachine {
    /// <summary>
    /// An object is a heap-allocated object. It can represent a string, function, class, etc.
    /// </summary>
    class GearsObj {
        public ObjType Type;
        public bool IsMarked = false;

        public enum ObjType {
            ObjBoundMethod,
            ObjClass,
            ObjClosure,
            ObjFunction,
            ObjInstance,
            ObjNative,
            ObjString,
            ObjUpvalue
        }

        public override string ToString() => "GearsObj";
    }

    class GearsObjBoundMethod : GearsObj {
        public readonly GearsValue Receiver;
        public readonly GearsObjClosure Method;

        public GearsObjBoundMethod(GearsValue receiver, GearsObjClosure method) {
            Type = ObjType.ObjBoundMethod;
            Receiver = receiver;
            Method = method;
        }

        public override string ToString() => Method.Function.ToString();
    }

    class GearsObjClass : GearsObj {
        public readonly string Name;
        public readonly GearsHashTable Methods;

        public GearsObjClass(string name) {
            Type = ObjType.ObjClass;
            Name = name;
            Methods = new GearsHashTable();
        }

        public override string ToString() => $"{Name}";
    }

    class GearsObjClosure : GearsObj {
        public readonly GearsObjFunction Function;
        public readonly GearsObjUpvalue[] Upvalues;

        public GearsObjClosure(GearsObjFunction fn, int upvalueCount) {
            Type = ObjType.ObjClosure;
            Function = fn;
            Upvalues = new GearsObjUpvalue[upvalueCount];
        }

        public override string ToString() => Function.ToString();
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

        /// <summary>
        /// The executable code and constants associated with this function.
        /// </summary>
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
        public GearsObjFunction(Gears context, string name, int arity) {
            Type = ObjType.ObjFunction;
            Name = name;
            Arity = arity;
            int index = context.ReadShort();
            Chunk = new GearsChunk(Name,
                context.Chunk.ReadConstantBytes(ref index),
                context.Chunk.ReadConstantBytes(ref index),
                context.Chunk.ReadConstantBytes(ref index));
        }

        /// <summary>
        /// Serialize to a chunk's constant storage...
        /// ... will be moved to code later.
        /// </summary>
        internal int Serialize(GearsChunk writer) {
            int constantIndex = writer.ConstantSize;
            Chunk.Compress();
            writer.WriteConstantShort(Chunk.CodeSize);
            if (Chunk.CodeSize > 0) {
                writer.WriteConstantBytes(Chunk._Code);
            }
            writer.WriteConstantShort(Chunk.ConstantSize);
            if (Chunk.ConstantSize > 0) {
                writer.WriteConstantBytes(Chunk._Constants);
            }
            writer.WriteConstantShort(Chunk.StringTableSize);
            if (Chunk.StringTableSize > 0) {
                writer.WriteConstantBytes(Chunk._StringTable);
            }
            return constantIndex;
        }

        public override string ToString() => Name == null ? "<script>" : $"<fn {Name}>";
    }

    class GearsObjFunctionNative : GearsObj {
        public readonly string Name;

        /// <summary>
        /// The number of parameters expected by the function.
        /// </summary>
        public int Arity;

        private readonly GearsFunctionNativeDelegate _OnInvoke;

        public GearsObjFunctionNative(string name, int arity, GearsFunctionNativeDelegate onInvoke) {
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

    delegate GearsValue GearsFunctionNativeDelegate(GearsValue[] args);

    class GearsObjInstance : GearsObj {
        public readonly GearsObjClass Class;
        public readonly GearsHashTable Fields;

        public GearsObjInstance(GearsObjClass classObj) {
            Type = ObjType.ObjInstance;
            Class = classObj;
            Fields = new GearsHashTable();
        }

        public override string ToString() => $"instance of {Class}";
    }

    class GearsObjString : GearsObj {
        public readonly string Value;

        public GearsObjString(string value) {
            Type = ObjType.ObjString;
            Value = value;
        }

        public override string ToString() => Value;
    }

    class GearsObjUpvalue : GearsObj {
        public GearsValue Value;
        public GearsObjUpvalue Next = null;
        public bool IsClosed = false;
        public int OriginalSP;

        public GearsObjUpvalue(int sp) {
            Type = ObjType.ObjUpvalue;
            OriginalSP = sp;
        }

        public override string ToString() => $"<upvalue {Value}>";
    }
}
