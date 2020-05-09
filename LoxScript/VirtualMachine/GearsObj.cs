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
        public readonly GearsObjFunction Method;

        public GearsObjBoundMethod(GearsValue receiver, GearsObjFunction method) {
            Type = ObjType.ObjBoundMethod;
            Receiver = receiver;
            Method = method;
        }

        public override string ToString() => Method.ToString();
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

    class GearsObjClassInstance : GearsObj {
        public readonly GearsObjClass Class;
        public readonly GearsHashTable Fields;

        public GearsObjClassInstance(GearsObjClass classObj) {
            Type = ObjType.ObjInstance;
            Class = classObj;
            Fields = new GearsHashTable();
        }

        public override string ToString() => $"instance of {Class}";
    }

    /// <summary>
    /// A lox function with enclosed scope and upvalues.
    /// </summary>
    class GearsObjFunction : GearsObj {
        /// <summary>
        /// The number of parameters expected by the function.
        /// </summary>
        public int Arity;

        /// <summary>
        /// The executable code and constants associated with this function.
        /// </summary>
        public readonly GearsChunk Chunk;

        public readonly int IP;

        public readonly GearsObjUpvalue[] Upvalues;

        public GearsObjFunction(GearsChunk chunk, int arity, int upvalueCount, int ip = 0) {
            Type = ObjType.ObjFunction;
            Chunk = chunk;
            Arity = arity;
            IP = ip;
            Upvalues = new GearsObjUpvalue[upvalueCount];
        }

        public override string ToString() => "<fn>";
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
