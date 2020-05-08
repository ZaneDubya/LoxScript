namespace LoxScript.VirtualMachine {
    class GearsCallFrame {
        public readonly GearsObjFunction Function;

        public int IP;

        public int BP;

        public GearsCallFrame(GearsObjFunction function, int ip = 0, int bp = 0)  {
            IP = ip;
            BP = bp;
            Function = function;
        }
    }
    /*
    /// <summary>
    /// A closure is a function that references local variables from enclosing scopes. These surrounding variables
    /// are 'closed over' with upvalues. Upvalues are a level of indirection needed to find a captured local variable
    /// even after it moves off the stack. A closure has a list of all upvalues used by the closure.
    /// </summary>
    class GearsCallFrameClosure : GearsCallFrame {
        public readonly GearsObjClosure Closure;

        public override GearsObjFunction Function => Closure.Function;

        public GearsCallFrameClosure(GearsObjClosure closure, int ip = 0, int bp = 0)
            : base(ip, bp) {
            Closure = closure;
        }
    }
    */
}
