using System;
using System.Collections.Generic;
using XPT.Core.Scripting.Base;
using XPT.Core.Utilities;
using static XPT.Core.Scripting.Base.TokenTypes;

namespace XPT.Core.Scripting.Rules.Compiling {
    internal static class RuleCompiler {

        private static readonly RuleTokenizer _Tokenizer = new RuleTokenizer();

        /// <summary>
        /// Compiles a single rule definition, outputting a named Trigger and list of Conditions.
        /// Returns true if successfully compiled, false otherwise.
        /// Definition always begins with named Trigger and then Conditions. Enclosing brackets are optional.
        /// Example: "OnEnterLocale LocaleID==4 PlayerLevel>=4"
        /// </summary>
        internal static bool TryCompile(string definition, out string trigger, out RuleCondition[] conditions) {
            try {
                _Tokenizer.Reset($"Rule", definition);
                TokenList tokens = _Tokenizer.ScanTokens();
                return TryCompile(tokens, out trigger, out conditions);
            }
            catch (Exception e) {
                throw new CompilerException(null, $"RuleCompiler: failed to parse '{definition}': {e.Message}");
            }
        }

        internal static bool TryCompile(TokenList tokens, out string trigger, out RuleCondition[] conditions) {
            Token triggerName = tokens.Consume(IDENTIFIER, $"Expect trigger name.");
            List<RuleCondition> conditionsList = new List<RuleCondition>();
            while (!tokens.Match(RIGHT_BRACKET) && !tokens.IsAtEnd()) {
                Token contextVariableName = tokens.Consume(IDENTIFIER, "Rule must contain list of comparison expressions (missing identifier).");
                Token comparisonOperation = tokens.Advance(); // we will check validity of this token after consuming the value
                Token contextVariableValue = null;
                if (tokens.Check(NUMBER) || tokens.Check(STRING)) {
                    contextVariableValue = tokens.Advance();
                }
                else {
                    throw new CompilerException(tokens.Peek(), "Rule must contain list of comparison expressions (missing value)");
                }
                switch (comparisonOperation.Type) {
                    case BANG_EQUAL:
                        throw new CompilerException(comparisonOperation, "Rule must contain list of comparison expressions (can't use != operator).");
                    case EQUAL:
                    case EQUAL_EQUAL:
                        if (contextVariableValue.Type == STRING) {
                            conditionsList.Add(RuleCondition.ConditionEquals(contextVariableName.Lexeme, contextVariableValue.LiteralAsString));
                        }
                        else {
                            conditionsList.Add(RuleCondition.ConditionEquals(contextVariableName.Lexeme, contextVariableValue.LiteralAsNumber));
                        }
                        break;
                    case GREATER:
                        conditionsList.Add(RuleCondition.ConditionGreaterThan(contextVariableName.Lexeme, contextVariableValue.LiteralAsNumber));
                        break;
                    case GREATER_EQUAL:
                        conditionsList.Add(RuleCondition.ConditionGreaterThanOrEqual(contextVariableName.Lexeme, contextVariableValue.LiteralAsNumber));
                        break;
                    case LESS:
                        conditionsList.Add(RuleCondition.ConditionLessThan(contextVariableName.Lexeme, contextVariableValue.LiteralAsNumber));
                        break;
                    case LESS_EQUAL:
                        conditionsList.Add(RuleCondition.ConditionLessThanOrEqual(contextVariableName.Lexeme, contextVariableValue.LiteralAsNumber));
                        break;
                    default:
                        throw new CompilerException(comparisonOperation, "Rule must contain list of comparison expressions (missing operator).");
                }
            }
            trigger = triggerName.Lexeme;
            conditions = conditionsList.ToArray();
            return true;
        }
    }
}
