using LoxScript.Grammar;
using LoxScript.Interpreter;
using LoxScript.Parsing;
using System;
using System.Collections.Generic;
using System.IO;

namespace LoxScript {
    class Program {
        private static Engine _Interpreter = new Engine();
        private static bool _HadError = false;
        private static bool _HadRuntimeError = false;

        static void Main(string[] args) {
            // Gears gears = new Gears();
            args = new string[] { "../../../Tests/test.txt" };
            if (args.Length > 1) {
                Console.WriteLine("Usage: loxscript [script]");
                Exit(64);
            }
            else if (args.Length == 1) {
                RunFile(args[0]);
                Exit(0, true);
            }
            else {
                RunPrompt();
            }
        }

        private static void Exit(int code, bool waitForKey = false) {
            if (waitForKey) {
                Console.ReadKey();
            }
            Environment.Exit(code);
        }

        private static void RunFile(string path) {
            string source = ReadFile(path);
            Run(source);
            if (_HadError) {
                Exit(65, true);
            }
            if (_HadRuntimeError) {
                Exit(70, true);
            }
        }

        private static void RunPrompt() {
            while (true) {
                Console.Write("> ");
                Run(Console.ReadLine());
                _HadError = false;
            }
        }

        private static void Run(string source) {
            TokenList tokens = new Scanner(source).ScanTokens();
            List<Stmt> statements = new Parser(tokens).Parse();
            // Stop if there was a syntax error.
            if (_HadError) {
                return;
            }
            EngineResolver resolver = new EngineResolver(_Interpreter);
            resolver.Resolve(statements);
            if (_HadError) {
                return;
            }
            _Interpreter.Interpret(statements);
        }

        // === Helpers ===============================================================================================
        // ===========================================================================================================

        private static string ReadFile(string path) {
            if (!File.Exists(path)) {
                Console.WriteLine("File does not exist.");
                Exit(64);
            }
            try {
                return File.ReadAllText(path);
            }
            catch {
                Console.WriteLine("Error reading file.");
                Exit(64);
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
            if (token.Type == TokenType.EOF) {
                Report(token.Line, " at end", message);
            }
            else {
                Report(token.Line, $" at '{token.Lexeme}'", message);
            }
        }

        public static void RuntimeError(Engine.RuntimeException error) {
            Console.Error.WriteLine($"[line {error.Token.Line}] Runtime Error: {error.Message}");
            _HadRuntimeError = true;
        }

        private static void Report(int line, string where, string message) {
            Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
            _HadError = true;
        }
    }
}
