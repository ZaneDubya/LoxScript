using System;
using System.Collections.Generic;

namespace XPT {
    class SharedBuffer : IDisposable {
        internal static int CountBuffersInUse => _BuffersInUse.Count;
        internal const int SizeSmallBuffer = 16384; // 1024 * 16
        internal const int SizeLargeBuffer = 1048576; // 1024 * 1024

        private static readonly object _LockRoot = new object();
        private static readonly List<byte[]> _Buffers = new List<byte[]>();
        private static readonly List<byte[]> _BuffersInUse = new List<byte[]>();
        private const int SizeMax = SizeLargeBuffer; // 1mb

        private static byte[] Get(int size, bool overrideSizeLimit = false) {
            byte[] buffer;
            lock (_LockRoot) {
                if (size <= SizeSmallBuffer) {
                    for (int i = 0; i < _Buffers.Count; i++) {
                        if (_Buffers[i].Length == SizeSmallBuffer) {
                            buffer = _Buffers[i];
                            _Buffers.RemoveAt(i);
                            _BuffersInUse.Add(buffer);
                            return buffer;
                        }
                    }
                    buffer = new byte[SizeSmallBuffer];
                    return buffer;
                }
                else if (size <= SizeLargeBuffer) {
                    for (int i = 0; i < _Buffers.Count; i++) {
                        if (_Buffers[i].Length == SizeLargeBuffer) {
                            buffer = _Buffers[i];
                            _Buffers.RemoveAt(i);
                            _BuffersInUse.Add(buffer);
                            return buffer;
                        }
                    }
                    buffer = new byte[SizeLargeBuffer];
                    return buffer;
                }
                else {
                    int desiredSize = SizeLargeBuffer;
                    while (desiredSize < size) {
                        desiredSize *= 2;
                    }
                    if (desiredSize > SizeMax && !overrideSizeLimit) {
                        throw new Exception($"XPT.Core.Buffer: cannot instance a buffer of size {size} bytes.");
                    }
                    for (int i = 0; i < _Buffers.Count; i++) {
                        if (_Buffers[i].Length >= desiredSize) {
                            buffer = _Buffers[i];
                            _Buffers.RemoveAt(i);
                            _BuffersInUse.Add(buffer);
                            return buffer;
                        }
                    }
                    buffer = new byte[desiredSize];
                    return buffer;
                }
            }
        }

        private static void Retire(byte[] buffer) {
            lock (_LockRoot) {
                if (_BuffersInUse.Contains(buffer)) {
                    _BuffersInUse.Remove(buffer);
                }
                if (!_Buffers.Contains(buffer)) {
                    _Buffers.Add(buffer);
                }
            }
        }

        // === Instance ===================================================================================================
        // ================================================================================================================

        private bool _IsDisposed = false;
        private readonly int _Length;
        private byte[] _Buffer;
        private string _Name;

        internal byte[] Buffer => _Buffer;

        internal int Length => _Buffer != null ? _Length : 0;

        internal byte this[int index] {
            get => _Buffer[index];
            set => _Buffer[index] = value;
        }

        internal SharedBuffer(string name, int desiredSize, bool overrideSizeLimit = false) {
            _Name = name;
            _Length = desiredSize;
            _Buffer = Get(desiredSize, overrideSizeLimit);
        }

        public override string ToString() => $"{(_Name ?? "Not in use")} ({_Buffer.Length} bytes)";

        protected virtual void Dispose(bool disposing) {
            if (!_IsDisposed) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }
                if (_Buffer != null) {
                    Retire(_Buffer);
                    _Buffer = null;
                }
                _IsDisposed = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~SharedBuffer() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
