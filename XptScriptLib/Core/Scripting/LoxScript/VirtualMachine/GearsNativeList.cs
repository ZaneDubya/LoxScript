using System.Collections.Generic;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// todo: not used
    /// </summary>
    class GearsNativeList<T> {
        internal readonly List<T> List;

        internal GearsNativeList() {
            List = new List<T>();
        }

        public int Count => List.Count;

        public T Get(int index) {
            if (index < 0 || index >= Count) {
                return default;
            }
            return List[index];
        }

        public override string ToString() => $"List of {typeof(T).Name}";
    }
}
