namespace GenerateGrammar {
    internal class GrammarCollection {
        internal readonly string Name;
        internal readonly bool VisitorIsTyped;
        /// <summary>
        /// description of each type: name of the class followed by : and the list of fields, separated by commas. Each field has a type and name.
        /// </summary>
        internal readonly string[] TypeDefs;

        public GrammarCollection(string name, bool visitorIsTyped, string[] typeDefs) {
            Name = name;
            VisitorIsTyped = visitorIsTyped;
            TypeDefs = typeDefs;
        }
    }
}