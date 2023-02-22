using System.Collections.Generic;
using System.Linq;
using XPT.Core.Scripting.Rules;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    internal partial class Gears { // all state variables and ctor

        // constants:

        private static readonly string InitString = "init";
        private const int FRAMES_MAX = 32;
        private const int HEAP_MAX = 256;
        private const int STACK_MAX = 256;

        // private variables:

        private int _FrameCount = 0;
        private readonly GearsCallFrame[] _Frames;
        private readonly Queue<GearsObj> _GrayList = new Queue<GearsObj>();
        private readonly GearsObj[] _Heap;
        private RuleCollection _Rules = null;
        private readonly GearsValue[] _Stack;

        private GearsObjUpvalue _OpenUpvalues = null; // reference to open upvariables
        private GearsCallFrame _OpenFrame => _Frames[_FrameCount - 1]; // references the current Frame
        private int _BP;
        private int _IP;
        private int _SP;
        private byte[] _Code;

        internal GearsChunk Chunk; // reference to current chunk
        internal readonly GearsHashTable Globals;
        internal GearsValue LastReturnValue;

        internal Gears() {
            _Frames = new GearsCallFrame[FRAMES_MAX];
            _Stack = new GearsValue[STACK_MAX];
            _Heap = new GearsObj[HEAP_MAX];
            Globals = new GearsHashTable();
        }

        internal string GetStatusString() {
            CollectGarbage();
            return $"BP={_BP} SP={_SP} IP={_IP} Frames={_FrameCount}/{FRAMES_MAX} Heap={_Heap.Where(d => d != null).Count()}/{HEAP_MAX}";
        }
    }
}
