using System.Xml.Linq;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// An object is a heap-allocated object. It can represent a string, function, class, etc.
    /// </summary>
    internal class GearsObj {
        public bool IsMarked = false;

        /// <summary>
        /// Call this to blacken for garbage collection.
        /// </summary>
        public virtual void Blacken(Gears vm) { }

        public override string ToString() => "GearsObj";
    }

    internal class GearsObjBoundMethod : GearsObj {
        public readonly GearsValue Receiver;
        public readonly GearsObjFunction Method;

        public GearsObjBoundMethod(GearsValue receiver, GearsObjFunction method) {
            Receiver = receiver;
            Method = method;
        }

        public override void Blacken(Gears vm) {
            vm.MarkValue(Receiver);
            vm.MarkObject(Method);
        }

        public override string ToString() => Method.ToString();
    }

    internal class GearsObjClass : GearsObj {
        public readonly string Name;
        public readonly GearsHashTable Methods;

        public GearsObjClass(string name) {
            Name = name;
            Methods = new GearsHashTable();
        }

        public override void Blacken(Gears vm) {
            vm.MarkTable(Methods);
        }

        public override string ToString() => $"{Name}";
    }

    internal abstract class GearsObjInstance : GearsObj {
        public abstract bool TryGetField(string name, out GearsValue value);
        public abstract void SetField(string name, GearsValue value);
    }

    /// <summary>
    /// An instance of a lox class. Unlike InstanceNative, you can add new fields to this instance.
    /// </summary>
    internal class GearsObjInstanceLox : GearsObjInstance {
        public readonly GearsObjClass Class;

        private readonly GearsHashTable _Fields;

        public GearsObjInstanceLox(GearsObjClass classObj) {
            Class = classObj;
            _Fields = new GearsHashTable();
        }

        public override void Blacken(Gears vm) {
            vm.MarkObject(Class);
            vm.MarkTable(_Fields);
        }

        public override bool TryGetField(string name, out GearsValue value) => _Fields.TryGet(name, out value);

        public override void SetField(string name, GearsValue value) => _Fields.Set(name, value);

        public override string ToString() => $"instance of {Class}";
    }

    /// <summary>
    /// A wrapper around an instance of a native class. Unlike InstanceLox, you cannot add new fields to this instance.
    /// </summary>
    internal class GearsObjInstanceNative : GearsObjInstance {
        public readonly object WrappedObject;

        private readonly Gears _Context;
        private readonly GearsNativeWrapper _Wrapper;

        public GearsObjInstanceNative(Gears vm, object wrappedObject) {
            _Context = vm;
            WrappedObject = wrappedObject;
            _Wrapper = GearsNativeWrapper.GetWrapper(wrappedObject.GetType());
        }

        public override void SetField(string name, GearsValue value) {
            _Wrapper.SetField(_Context, WrappedObject, name, value);
        }

        public override bool TryGetField(string name, out GearsValue value) {
            return _Wrapper.TryGetField(_Context, WrappedObject, name, out value);
        }

        public override string ToString() => $"instance of {WrappedObject.GetType().Name}";
    }

    /// <summary>
    /// A lox function with enclosed scope and upvalues.
    /// </summary>
    internal class GearsObjFunction : GearsObj {
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
            Chunk = chunk;
            Arity = arity;
            IP = ip;
            Upvalues = new GearsObjUpvalue[upvalueCount];
        }

        public override void Blacken(Gears vm) {
            foreach (GearsObjUpvalue upvalue in Upvalues) {
                vm.MarkObject(upvalue);
            }
        }

        public override string ToString() => "<fn>";
    }

    internal class GearsObjFunctionNative : GearsObj {
        public readonly string Name;

        /// <summary>
        /// The number of parameters expected by the function.
        /// </summary>
        public int Arity;

        private readonly GearsFunctionNativeDelegate _OnInvoke;

        public GearsObjFunctionNative(string name, int arity, GearsFunctionNativeDelegate onInvoke) {
            Name = name;
            Arity = arity;
            _OnInvoke = onInvoke;
        }

        public GearsValue Invoke(params GearsValue[] args) {
            return _OnInvoke(args);
        }

        public override string ToString() => $"<native {Name}>";
    }

    internal delegate GearsValue GearsFunctionNativeDelegate(GearsValue[] args);

    internal class GearsObjString : GearsObj {
        public readonly string Value;

        public GearsObjString(string value) {
            Value = value;
        }

        public override string ToString() => Value;
    }

    internal class GearsObjUpvalue : GearsObj {
        public GearsValue Value;
        public GearsObjUpvalue Next = null;
        public bool IsClosed = false;
        public int OriginalSP;

        public GearsObjUpvalue(int sp) {
            OriginalSP = sp;
        }

        public override void Blacken(Gears vm) {
            vm.MarkValue(Value);
        }

        public override string ToString() => $"<upvalue {Value}>";
    }
}
