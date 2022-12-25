using System.Collections.Generic;
using System.Linq;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    internal partial class Gears { // all state variables, ctor

        private readonly GearsCallFrame[] _Frames;
        private GearsObj[] _Heap;
        private readonly GearsValue[] _Stack;
        private int _FrameCount = 0;
        private readonly Queue<GearsObj> _GrayList = new Queue<GearsObj>();
        protected GearsObjUpvalue _OpenUpvalues = null; // reference to open upvariables
        protected GearsCallFrame _OpenFrame => _Frames[_FrameCount - 1]; // references the current Frame
        protected int _BP;
        protected int _IP;
        protected int _SP;
        protected byte[] _Code;
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
