using System.Collections.Generic;
using XPT.Core.Scripting.Base;
using static XPT.Core.Scripting.Base.TokenTypes;
using static XPT.Core.Scripting.LoxScript.Compiling.LoxTokenTypes;
using static XPT.Core.Scripting.LoxScript.VirtualMachine.EGearsOpCode;

namespace XPT.Core.Scripting.LoxScript.Compiling {
    internal sealed partial class LoxCompiler {

        /// <summary>
        /// Follows 'switch' token.
        /// </summary>
        private void SwitchStatement() {
            Token switchToken = Tokens.Previous();
            if (_InSwitchStatement) {
                throw new CompilerException(Tokens.Previous(), "Nested switch statements are not allowed.");
            }
            _InSwitchStatement = true; // don't allow nested switches

            // parse the switch condition, following 'switch' token:
            Tokens.Consume(LEFT_PAREN, "Expect '(' after 'switch'.");
            Expression(); // and push results
            Tokens.Consume(RIGHT_PAREN, "Expect ')' after switch condition.");
            Tokens.Consume(LEFT_BRACE, "Expect '{' after switch condition.");

            Token defaultStatement = null;
            int defaultStatementCodeBodyTokenIndex = -1;
            List<Token> caseStatements = new List<Token>();
            List<int> caseStatementCodeBodyTokenIndexes = new List<int>();
            int defaultStatementJumpIndex = -1;
            List<int> caseStatementJumpIndexes = new List<int>();
            List<int> breakStatementJumpIndexes = new List<int>();
            bool inCodeStatement = false;

            // Get token indexes of all the case comparisons, and the default statement (if any). Each case statement
            // or default statement is followed by a code body, so get indexes to these as well.
            int bookmarkSwitchBegin = Tokens.CurrentIndex;
            while (!Tokens.Check(RIGHT_BRACE) && !Tokens.IsAtEnd()) {
                if (Tokens.Match(SWITCH)) {
                    throw new CompilerException(Tokens.Previous(), "Nested switch statements are not allowed.");
                }
                if (Tokens.Match(CASE, DEFAULT)) {
                    inCodeStatement = false;
                    bool isDefault = Tokens.Previous().Type == DEFAULT;
                    if (isDefault) {
                        if (defaultStatement != null) {
                            throw new CompilerException(Tokens.Previous(), "Multiple default cases in a switch are not allowed.");
                        }
                        defaultStatement = Tokens.Previous();
                        defaultStatementCodeBodyTokenIndex = -1;
                    }
                    else {
                        caseStatements.Add(Tokens.Consume(NUMBER, "Expect numeric value following case statement."));
                        caseStatementCodeBodyTokenIndexes.Add(-1);
                    }
                    Tokens.Consume(COLON, "Expect ':' after case or default statement.");
                }
                else {
                    if (!inCodeStatement) {
                        if (defaultStatement != null && defaultStatementCodeBodyTokenIndex == -1) {
                            defaultStatementCodeBodyTokenIndex = Tokens.CurrentIndex;
                        }
                        for (int i = 0; i < caseStatementCodeBodyTokenIndexes.Count; i++) {
                            if (caseStatementCodeBodyTokenIndexes[i] == -1) {
                                caseStatementCodeBodyTokenIndexes[i] = Tokens.CurrentIndex;
                            }
                        }
                    }
                    inCodeStatement = true;
                    int scope = 0;
                    while (scope > 0 || (!Tokens.Check(CASE) && !Tokens.Check(DEFAULT) && !Tokens.Check(RIGHT_BRACE))) {
                        if (Tokens.Check(LEFT_BRACE)) {
                            scope += 1;
                        }
                        if (Tokens.Check(RIGHT_BRACE)) {
                            scope -= 1;
                        }
                        Tokens.Advance();
                    }
                }
            }
            Tokens.Consume(RIGHT_BRACE, "Switch statement must end with right brace.");
            int bookmarkSwitchEnd = Tokens.CurrentIndex;
            Tokens.CurrentIndex = bookmarkSwitchBegin;

            // The results of the switch condition are on the stack. we need to compare it to each case value.
            // We will do this with the equivalent of a series of if-else if statements, with a final else
            // statement for the default statement, if any. The structure of the emitted opcodes will be:
            // For each case statement ...
            //      OP_LOAD_CONSTANT
            //      <numeric constant index of case value>
            //      OP_EQUAL_PRESERVE_FIRST_VALUE
            //      OP_JUMP_IF_FALSE (to next case comparison)
            //      OP_POP
            //      OP_JUMP (to code body for this case)
            // ... more case statments ...
            // ... finally, if there is a default statement ...
            //      OP_JUMP (to code body for default)
            // ... or if there is no default statement ...
            //      OP_JUMP (to end of switch)

            // Emit code for the case and default statements:
            for (int i = 0; i < caseStatements.Count; i++) {
                bool isLast = (defaultStatement == null) && (i == caseStatements.Count - 1);
                Token caseValue = caseStatements[i];
                if (i > 0) {
                    EmitOpcode(caseValue.Line, OP_POP);
                }
                EmitOpcode(caseValue.Line, OP_LOAD_CONSTANT);
                EmitConstantIndex(caseValue.Line, MakeValueConstant(caseValue.LiteralAsNumber), _FixupConstants);
                EmitOpcode(caseValue.Line, OP_EQUAL_PRESERVE_FIRST_VALUE);
                // If not equal...
                // (a) if not last, jump to next case statement,
                // (b) if last, jump to default statement, or if no default statement, jump to end of switch.
                int jumpNextCase = EmitJump(OP_JUMP_IF_FALSE);
                if (isLast) {
                    breakStatementJumpIndexes.Add(jumpNextCase);
                }
                EmitOpcode(caseValue.Line, OP_POP); // Pop the result of the comparison
                int jumpCodeBody = EmitJump(OP_JUMP);
                caseStatementJumpIndexes.Add(jumpCodeBody);
                if (!isLast) {
                    PatchJump(jumpNextCase);
                }
            }
            if (defaultStatement != null) {
                if (caseStatements.Count > 0) {
                    EmitOpcode(defaultStatement.Line, OP_POP); // Pop the result of the last case comparison
                }
                defaultStatementJumpIndex = EmitJump(OP_JUMP);
            }

            // Get a list of unique code bodies handled by the switch statement.
            List<int> uniqueCodeBodyTokenIndexes = new List<int>();
            foreach (int codeBodyIndex in caseStatementCodeBodyTokenIndexes) {
                if (!uniqueCodeBodyTokenIndexes.Contains(codeBodyIndex)) {
                    uniqueCodeBodyTokenIndexes.Add(codeBodyIndex);
                }
            }
            if (defaultStatement != null && !uniqueCodeBodyTokenIndexes.Contains(defaultStatementCodeBodyTokenIndex)) {
                uniqueCodeBodyTokenIndexes.Add(defaultStatementCodeBodyTokenIndex);
            }

            // For each unique code body, emit the code for the code body, and patch jumps to it.
            int maxCodeBodyIndex = -1;
            foreach (int codeBodyIndex in uniqueCodeBodyTokenIndexes) {
                if (codeBodyIndex == -1) {
                    throw new CompilerException(switchToken, "Empty case or default statement in switch statement.");
                }
                if (maxCodeBodyIndex > codeBodyIndex) {
                    continue;
                }
                SwitchStatement_PatchCase(caseStatementCodeBodyTokenIndexes, caseStatementJumpIndexes, codeBodyIndex);
                SwitchStatement_PatchDefault(defaultStatement, defaultStatementCodeBodyTokenIndex, defaultStatementJumpIndex, codeBodyIndex);
                Tokens.CurrentIndex = codeBodyIndex;
                while (!Tokens.Check(BREAK) && !Tokens.Check(RIGHT_BRACE)) {
                    if (Tokens.Match(CASE)) {
                        Tokens.Consume(NUMBER, "Expect numeric value following case statement.");
                        Tokens.Consume(COLON, "Expect ':' after case statement.");
                        SwitchStatement_PatchCase(caseStatementCodeBodyTokenIndexes, caseStatementJumpIndexes, Tokens.CurrentIndex);
                    }
                    else if (Tokens.Match(DEFAULT)) {
                        Tokens.Consume(COLON, "Expect ':' after default statement.");
                        SwitchStatement_PatchDefault(defaultStatement, defaultStatementCodeBodyTokenIndex, defaultStatementJumpIndex, Tokens.CurrentIndex);
                        SwitchStatement_PatchCase(caseStatementCodeBodyTokenIndexes, caseStatementJumpIndexes, Tokens.CurrentIndex);
                    }
                    else {
                        Statement();
                    }
                }
                if (Tokens.Match(BREAK)) {
                    breakStatementJumpIndexes.Add(EmitJump(OP_JUMP));
                }
                maxCodeBodyIndex = Tokens.CurrentIndex;
            }
            foreach (int breakStatementJumpIndex in breakStatementJumpIndexes) {
                PatchJump(breakStatementJumpIndex);
            }
            EmitOpcode(switchToken.Line, OP_POP); // Pop the result of the switch comparison
            Tokens.CurrentIndex = bookmarkSwitchEnd;
            _InSwitchStatement = false;
        }

        private void SwitchStatement_PatchCase(List<int> caseStatementCodeBodyTokenIndexes, List<int> caseStatementJumpIndexes, int currentToken) {
            for (int i = 0; i < caseStatementCodeBodyTokenIndexes.Count; i++) {
                if (caseStatementCodeBodyTokenIndexes[i] == currentToken) {
                    PatchJump(caseStatementJumpIndexes[i]);
                }
            }
        }

        private void SwitchStatement_PatchDefault(Token defaultStatement, int defaultStatementCodeBodyTokenIndex, int defaultStatementJumpIndex, int currentToken) {
            if (defaultStatement != null && defaultStatementCodeBodyTokenIndex == currentToken) {
                PatchJump(defaultStatementJumpIndex);
            }
        }
    }
}
