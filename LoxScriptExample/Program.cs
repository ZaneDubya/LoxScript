using System;
using System.Diagnostics;
using System.IO;
using XPT.Core.IO;
using XPT.Core.Scripting.Base;
using XPT.Core.Scripting.LoxScript;
using XPT.Core.Scripting.LoxScript.Compiling;
using XPT.Core.Scripting.LoxScript.VirtualMachine;

namespace XPT {
    class Program {
        private static bool _HadError = false;
        private static bool _HadRuntimeError = false;
        private const string StrUse = "Usage: loxscript [script]";
        private const string StrPrompt = "LoxScript:\n  1. Gears (bytecode vm) benchmark\n  2. Native benchmark\n  3. Test suite";

        private static void Main(string[] args) {
            if (args.Length > 1) {
                Console.WriteLine();
                Exit(64);
            }
            else if (args.Length == 1) {
                RunFile(args[0]);
                Exit(0, true);
            }
            else {
                while (true) {
                    Console.WriteLine(StrPrompt);
                    switch (Console.ReadKey(true).Key) {
                        case ConsoleKey.D1:
                            RunFile("../../../Tests/benchmark.lox");
                            break;
                        case ConsoleKey.D2:
                            RunNativeBenchmark();
                            break;
                        case ConsoleKey.D3:
                            RunFile("../../../Tests/testsuite.lox");
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private static void Exit(int code, bool waitForKey = false) {
            if (waitForKey) {
                Console.ReadKey();
            }
            Environment.Exit(code);
        }

        private static void RunFile(string path) {
            Run(path);
            if (_HadError) {
                Exit(65, true);
            }
            if (_HadRuntimeError) {
                Exit(70, true);
            }
        }

        private static void Run(string path) {
            string source = ReadFile(path);
            if (LoxCompiler.TryCompileFromSource(path, source, out GearsChunk chunk, out string status)) {
                using (BinaryFileWriter writer = new BinaryFileWriter("compiled.lxx")) {
                    chunk.Serialize(writer);
                    writer.Close();
                }
                Gears gears = new Gears();
                gears.Reset(chunk, false);
                gears.AddNativeObjectToGlobals("TestObj", new TestNativeObject());
                Gears.Disassemble(chunk, Console.Write, Console.WriteLine);
                Console.WriteLine("Press enter to run.");
                Console.ReadKey();
                gears.Run();
            }
            else {
                Console.WriteLine(status);
            }
        }

        // === Helpers ===============================================================================================
        // ===========================================================================================================

        private static string ReadFile(string path) {
            if (!File.Exists(path)) {
                Console.WriteLine("File does not exist.");
                Exit(64, true);
            }
            try {
                return File.ReadAllText(path);
            }
            catch {
                Console.WriteLine("Error reading file.");
                Exit(64, true);
                return null;
            }
        }

        // === Error Handling ========================================================================================
        // ===========================================================================================================

        public static void Error(int line, string message) {
            Report(line, "", message);
        }

        /// <summary>
        /// This reports an error, but does not by itself interupt the interpreter/parser flow.
        /// </summary>
        public static void Error(Token token, string message) {
            if (token.Type == TokenTypes.EOF) {
                Report(token.Line, " at end", message);
            }
            else {
                Report(token.Line, $" at '{token.Lexeme}'", message);
            }
        }

        private static void Report(int line, string where, string message) {
            Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
            _HadError = true;
        }

        // === Native Benchmark ======================================================================================
        // ===========================================================================================================

        private static void RunNativeBenchmark() {
            int total = 0;
            for (var j = 0; j < 10; j = j + 1) {
                int start = RunNativeBenchmarkClock();
                for (var i = 0; i < 30; i = i + 1) {
                    RunNativeBenchmarkFibonacci(i);
                }
                int now = RunNativeBenchmarkClock() - start;
                total = total + now;
                Console.WriteLine(j);
            }
            Console.WriteLine($"{total / 10:F2} ms");
        }

        private static int RunNativeBenchmarkFibonacci(int n) {
            if (n <= 1) {
                return n;
            }
            return RunNativeBenchmarkFibonacci(n - 2) + RunNativeBenchmarkFibonacci(n - 1);
        }

        private static int RunNativeBenchmarkClock() {
            return (int)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000L));
        }

    }
}
