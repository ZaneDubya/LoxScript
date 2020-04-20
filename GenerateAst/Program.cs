using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GenerateAst {
    class Program {
        private const string DefaultOutputDirectory = "../../../LoxScript/Grammar";

        static void Main(string[] args) {
            string outputDir = string.Empty;
            Action<string, string> outputAst = null;
            if (args.Length == 0) {
                // output to existing files
                outputDir = DefaultOutputDirectory;
                outputAst = OutputToFile;
            }
            else if (args.Length == 1) {
                // output to file
                outputDir = args[0];
                outputAst = OutputToFile;
            }
            else {
                Console.Error.WriteLine("Usage: GenerateAst [<output directory>]");
                Environment.Exit(1);
            }
            // description of each type; name of the class followed by : and the list of fields, separated by commas. Each field has a type and name.
            outputAst(Path.Combine(outputDir, "Expr.cs"), DefineAst("Expr", true, new string[] {
                "Assign     : Token name, Expr value",
                "Binary     : Expr left, Token op, Expr right",
                "Call       : Expr callee, Token paren, List<Expr> arguments",
                "Get        : Expr obj, Token name",
                "Grouping   : Expr expression",
                "Literal    : object value",
                "Logical    : Expr left, Token op, Expr right",
                "Super      : Token keyword, Token method",
                "Set        : Expr obj, Token name, Expr value",
                "This       : Token keyword",
                "Unary      : Token op, Expr right",
                "Variable   : Token name"
            }));
            outputAst(Path.Combine(outputDir, "Stmt.cs"), DefineAst("Stmt", false, new string[] {
                "Block      : List<Stmt> statements",
                "Class      : Token name, Expr.Variable superClass, List<Function> methods", // at runtime, the superclass variabl
                "Expres     : Expr expression",
                "Function   : Token name, List<Token> parameters, List<Stmt> body",
                "If         : Expr condition, Stmt thenBranch, Stmt elseBranch",
                "Print      : Expr expression",
                "Return     : Token keyword, Expr value",
                "Var        : Token name, Expr initializer",
                "While      : Expr condition, Stmt body"
            }));
        }

        // --- Output Methods ----------------------------------------------------------------------------------------

        private static void OutputToConsole(string path, string text) {
            Console.WriteLine(path);
            foreach(string line in text.Split('\n')) {
                Console.WriteLine(line);
            }
            Console.ReadKey();
        }

        private static void OutputToFile(string path, string text) {
            File.WriteAllText(path, text);
        }

        // --- Definition Methods ------------------------------------------------------------------------------------

        private static string DefineAst(string baseName, bool hasVisitorType, IEnumerable<string> types) {
            StringBuilder writer = new StringBuilder();
            writer.AppendLine("using LoxScript.Grammar;");
            writer.AppendLine("using System.Collections.Generic;");
            writer.AppendLine();
            writer.AppendLine("namespace LoxScript {");
            writer.AppendLine($"    abstract class {baseName} {{");
            writer.AppendLine();
            // the visitor interface:
            DefineVisitor(writer, baseName, hasVisitorType, types);
            // the base accept() method
            if (hasVisitorType) {
                writer.AppendLine($"        internal abstract T Accept<T>(IVisitor<T> visitor);");
            }
            else {
                writer.AppendLine($"        internal abstract void Accept(IVisitor visitor);");
            }
            writer.AppendLine();
            // The AST classes:
            foreach (string type in types) {
                GetTypeNameAndFieldsFromTypeDef(type, out string typeName, out string fields);
                DefineType(writer, baseName, typeName, hasVisitorType, fields);
            }
            writer.AppendLine("    }"); // close class namespace
            writer.AppendLine("}"); // close namespace
            return writer.ToString();
        }

        private static void DefineVisitor(StringBuilder writer, string baseName, bool hasVisitorType, IEnumerable<string> types) {
            if (hasVisitorType) {
                writer.AppendLine("        internal interface IVisitor<T> {");
            }
            else {
                writer.AppendLine("        internal interface IVisitor {");
            }
            foreach (string type in types) {
                GetTypeNameAndFieldsFromTypeDef(type, out string typeName, out string fields);
                writer.AppendLine($"            {(hasVisitorType ? "T" : "void")} Visit{typeName}{baseName}({typeName} {baseName.ToLowerInvariant()});");
            }
            writer.AppendLine("        }"); // close visitor
            writer.AppendLine();
        }

        private static void DefineType(StringBuilder writer, string baseName, string className, bool hasVisitorType, string fieldList) {
            // class:
            writer.AppendLine($"        internal class {className} : {baseName} {{");
            // fields:                  
            string[] fields = fieldList.Split(',');
            foreach (string field in fields) {
                GetFieldTypeAndNameFromFieldDef(field, out string type, out string name);
                writer.AppendLine($"            internal {type} {Capitalize(name)};");
            }
            writer.AppendLine();
            // ctor:
            writer.AppendLine($"            internal {className}({fieldList}) {{");
            foreach (string field in fields) {
                GetFieldTypeAndNameFromFieldDef(field, out string type, out string name);
                writer.AppendLine($"                {Capitalize(name)} = {name};");
            }
            writer.AppendLine("            }");
            writer.AppendLine();
            // visitor pattern:
            if (hasVisitorType) {
                writer.AppendLine($"            internal override T Accept<T>(IVisitor<T> visitor) {{");
                writer.AppendLine($"                return visitor.Visit{className}{baseName}(this);");
            }
            else {
                writer.AppendLine($"            internal override void Accept(IVisitor visitor) {{");
                writer.AppendLine($"                visitor.Visit{className}{baseName}(this);");
            }
            writer.AppendLine("            }");
            // end class
            writer.AppendLine("        }");
            writer.AppendLine();
        }

        // --- Helpers -----------------------------------------------------------------------------------------------

        private static void GetTypeNameAndFieldsFromTypeDef(string type, out string typeName, out string fields) {
            typeName = type.Split(':')[0].Trim();
            fields = type.Split(':')[1].Trim();
        }

        private static void GetFieldTypeAndNameFromFieldDef(string field, out string fieldType, out string fieldName) {
            fieldType = field.Trim().Split(' ')[0].Trim();
            fieldName = field.Trim().Split(' ')[1].Trim();
        }

        private static string Capitalize(string value) {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }
            if (value.Length > 1) {
                return char.ToUpperInvariant(value[0]) + value.Substring(1);
            }
            return value.ToUpperInvariant();
        }
    }
}
