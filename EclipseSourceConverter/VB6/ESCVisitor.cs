using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using EclipseSourceConverter.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Diagnostics;
using Antlr4.Runtime;

namespace EclipseSourceConverter.VB6
{
    class ESCVisitor : VisualBasic6BaseVisitor<IEnumerable<SyntaxNode>>
    {
        CompilationUnit compilationUnit;

        public ESCVisitor(string name, CompilationUnit compilationUnit) {
            this.compilationUnit = compilationUnit;

            compilationUnit.UsingDirectives.Add(compilationUnit.Generator.NamespaceImportDeclaration("System"));
        }

        protected override IEnumerable<SyntaxNode> DefaultResult {
            get {
                throw new NotSupportedException();
            }
        }

        public override IEnumerable<SyntaxNode> VisitModule([NotNull] VisualBasic6Parser.ModuleContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitStartRule([NotNull] VisualBasic6Parser.StartRuleContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitModuleBodyElement([NotNull] VisualBasic6Parser.ModuleBodyElementContext context) {
            var nodes = EnumerateChildElements(context);

            foreach (var node in nodes) {
                if (node != null) {
                    compilationUnit.Members.Add(node);
                    yield return node;
                }
            }
        }

        public override IEnumerable<SyntaxNode> Visit(IParseTree tree) {
            //var text = tree.GetText();
            //Console.WriteLine($"{text}: {tree.GetType()}");

            return base.Visit(tree);
        }

        private IEnumerable<SyntaxNode> EnumerateChildElements<T>(T context) where T : IParseTree {
            for (int i = 0; i < context.ChildCount; i++) {
                var childNodes = Visit(context.GetChild(i));
                foreach (var childNode in childNodes) {
                    yield return childNode;
                }
            }
        }

        public override IEnumerable<SyntaxNode> VisitModuleBlock([NotNull] VisualBasic6Parser.ModuleBlockContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitConstStmt([NotNull] VisualBasic6Parser.ConstStmtContext context) {
            var accessibility = DetermineAccessibility(context.GetChild<VisualBasic6Parser.VisibilityContext>(0));

            foreach (var constSubStmt in EnumerateContexts<VisualBasic6Parser.ConstSubStmtContext>(context)) {
                var name = constSubStmt.GetChild<VisualBasic6Parser.AmbiguousIdentifierContext>(0).GetText();

                bool isArray = false;

                var (finalTypeNode, baseTypeNode) = WalkTypeNode(constSubStmt.GetChild<VisualBasic6Parser.AsTypeClauseContext>(0), isArray);

                SyntaxNode initializerNode = null;

                if (constSubStmt.EQ() != null) {
                    var initializerValue = constSubStmt.GetChild<VisualBasic6Parser.ValueStmtContext>(0);

                    initializerNode = compilationUnit.Generator.GenerateNodeForLiteralOrName(initializerValue.GetText());
                }

                var fieldDeclaration = compilationUnit.Generator.FieldDeclaration(name,
                                                                                  finalTypeNode,
                                                                                  accessibility,
                                                                                  modifiers: DeclarationModifiers.Const,
                                                                                  initializer: initializerNode);

                yield return fieldDeclaration;
            }
        }

        public override IEnumerable<SyntaxNode> VisitVariableStmt([NotNull] VisualBasic6Parser.VariableStmtContext context) {
            var result = context.GetText();

            var accessibility = DetermineAccessibility(context.GetChild<VisualBasic6Parser.VisibilityContext>(0));

            var variableListStmt = context.GetChild<VisualBasic6Parser.VariableListStmtContext>(0);

            bool isArray = false;

            foreach (var subStatement in EnumerateContexts<VisualBasic6Parser.VariableSubStmtContext>(variableListStmt)) {
                var name = subStatement.GetChild<VisualBasic6Parser.AmbiguousIdentifierContext>(0).GetText();

                var leftParam = subStatement.LPAREN();
                var rightParam = subStatement.RPAREN();

                SyntaxNode subscriptNode = null;

                if (leftParam != null && rightParam != null) {
                    isArray = true;

                    // This is an array, get subscripts
                    var subscripts = subStatement.GetChild<VisualBasic6Parser.SubscriptsContext>(0);
                    subscriptNode = VisitSubscripts(subscripts).FirstOrDefault();
                }

                var (finalTypeNode, baseTypeNode) = WalkTypeNode(subStatement.GetChild<VisualBasic6Parser.AsTypeClauseContext>(0), isArray);

                SyntaxNode initializerNode = null;
                if (subscriptNode != null) {
                    initializerNode = compilationUnit.Generator.ArrayCreationExpression(baseTypeNode, subscriptNode);
                }

                SyntaxNode declarationNode;
                if (compilationUnit.IsParentAClass) {
                    declarationNode = compilationUnit.Generator.FieldDeclaration(name, finalTypeNode, accessibility,
                        initializer: initializerNode);
                } else {
                    declarationNode = compilationUnit.Generator.LocalDeclarationStatement(finalTypeNode, name, initializerNode);
                }
                yield return declarationNode;
            }
        }

        private IEnumerable<T> EnumerateContexts<T>(ParserRuleContext parentContext) where T : IParseTree {
            int i = 0;

            var childContext = parentContext.GetChild<T>(i++);
            while (childContext != null) {
                yield return childContext;

                childContext = parentContext.GetChild<T>(i++);
            }
        }

        private Accessibility DetermineAccessibility(string accessibility) {
            switch (accessibility.ToLower()) {
                case "public":
                    return Accessibility.Public;
                default:
                    return Accessibility.NotApplicable;
            }
        }

        private Accessibility DetermineAccessibility(VisualBasic6Parser.VisibilityContext visibilityContext) {
            if (visibilityContext != null) {
                return DetermineAccessibility(visibilityContext.GetText());
            } else {
                return Accessibility.NotApplicable;
            }
        }

        public override IEnumerable<SyntaxNode> VisitSubStmt([NotNull] VisualBasic6Parser.SubStmtContext context) {
            var accessibility = DetermineAccessibility(context.GetChild<VisualBasic6Parser.VisibilityContext>(0));

            var nameContext = context.GetChild<VisualBasic6Parser.AmbiguousIdentifierContext>(0);
            var name = nameContext.GetText();

            var methodContext = new MethodContext()
            {
                Accessibility = accessibility,
                Name = name
            };

            var blockContext = context.GetChild<VisualBasic6Parser.BlockContext>(0);
            if (blockContext == null) {
                // This is an empty method, skip it
                yield break;
            }

            compilationUnit.BlockCount++;

            var statements = EnumerateChildElements(blockContext);

            IEnumerable<SyntaxNode> argList = null;

            var argListContext = context.GetChild<VisualBasic6Parser.ArgListContext>(0);
            if (argListContext != null) {
                argList = VisitArgList(argListContext);
            }

            var methodDeclaration = compilationUnit.Generator.MethodDeclaration(name, parameters: argList, accessibility: accessibility, statements: statements);

            compilationUnit.BlockCount--;

            yield return methodDeclaration;
        }

        public override IEnumerable<SyntaxNode> VisitBlockStmt(VisualBasic6Parser.BlockStmtContext context) {
            compilationUnit.BlockCount++;
            foreach (var childNode in EnumerateChildElements(context)) {
                yield return childNode;
            }
            compilationUnit.BlockCount--;
        }

        public override IEnumerable<SyntaxNode> VisitInlineIfThenElse([NotNull] VisualBasic6Parser.InlineIfThenElseContext context) {
            var ifConditionStmt = context.GetChild<VisualBasic6Parser.IfConditionStmtContext>(0);
            var valueStmt = ifConditionStmt.GetChild<VisualBasic6Parser.ValueStmtContext>(0);
            var ifConditionNode = WalkValueStmt(valueStmt);

            var blockStmt = context.GetChild<VisualBasic6Parser.BlockStmtContext>(0);
            var blockStmtNodes = VisitBlockStmt(blockStmt);

            var elseBlockStmt = context.GetChild<VisualBasic6Parser.BlockStmtContext>(1);
            IEnumerable<SyntaxNode> elseBlockStmtNodes = null;
            if (elseBlockStmt != null) {
                elseBlockStmtNodes = VisitBlockStmt(elseBlockStmt);
            }

            var node = compilationUnit.Generator.IfStatement(ifConditionNode, blockStmtNodes, elseBlockStmtNodes);

            yield return node;
        }

        private SyntaxNode WalkValueStmt(VisualBasic6Parser.ValueStmtContext context) {
            var validChildren = context.children.Where((parseTree) =>
            {
                switch (parseTree) {
                    case VisualBasic6Parser.VsICSContext icsCtw:
                    case VisualBasic6Parser.VsLiteralContext vsLiteralCtx:
                    case VisualBasic6Parser.LiteralContext literalCtx:
                    case VisualBasic6Parser.VsAmpContext ampCtx:
                    case VisualBasic6Parser.VsAssignContext assignCtx:
                    case VisualBasic6Parser.ImplicitCallStmt_InStmtContext ctx:
                        return true;
                    case ITerminalNode terminalNode:
                        switch (terminalNode.Symbol.Text.ToLower()) {
                            case "&":
                            case "=":
                            case "+":
                            case "-":
                            case "<":
                            case ">":
                            case "and":
                            case "or":
                                return true;
                            case " ":
                                // Skip whitespace
                                return false;
                            default:
                                Announcer.Instance.Announce(AnnouncementType.Unimplemented, $"Terminal node: {terminalNode.Symbol.Text}");
                                return false;
                        }
                }

                return false;
            });

            Queue<IParseTree> childQueue = new Queue<IParseTree>(validChildren);

            IParseTree child;
            SyntaxNode currentNode = null;
            if (childQueue.Count > 0) {
                child = childQueue.Dequeue();

                currentNode = WalkBaseValueStatementNode(child);
            }

#if DEBUG
            if (currentNode == null) {
                Debug.WriteLine("Unknown value statement. Generating False");
                return compilationUnit.Generator.FalseLiteralExpression();
            }
#endif

            while (childQueue.Count > 0) {
                child = childQueue.Dequeue();

                switch (child) {
                    case VisualBasic6Parser.VsAssignContext assignCtx: {
                            var rightChild = childQueue.Dequeue();
                            var right = WalkBaseValueStatementNode(rightChild);

                            currentNode = compilationUnit.Generator.AssignmentStatement(currentNode, right);
                        }
                        break;
                    case ITerminalNode childCtx: {
                            if (childQueue.Count == 0) {
                                return currentNode;
                            }

                            var rightChild = childQueue.Dequeue();
                            var right = WalkBaseValueStatementNode(rightChild);

                            if (right == null) {
                                // TODO: Handle the case where right == null
                                Debug.WriteLine("Invalid \"right\" node");
                                var text = rightChild.GetText();
                                right = compilationUnit.Generator.NullLiteralExpression();
                            }

                            switch (childCtx.Symbol.Text.ToLower()) {
                                case "or": // Bitwise "or"
                                    currentNode = compilationUnit.Generator.BitwiseOrExpression(currentNode, right);
                                    break;
                                case "and": // Bitwise "and"
                                    currentNode = compilationUnit.Generator.BitwiseAndExpression(currentNode, right);
                                    break;
                                case "=":
                                    currentNode = compilationUnit.Generator.ValueEqualsExpression(currentNode, right);
                                    break;
                                case "+":
                                case "&":
                                    currentNode = compilationUnit.Generator.AddExpression(currentNode, right);
                                    break;
                                case "-":
                                    currentNode = compilationUnit.Generator.SubtractExpression(currentNode, right);
                                    break;
                                case "<":
                                    currentNode = compilationUnit.Generator.LessThanExpression(currentNode, right);
                                    break;
                                case ">":
                                    currentNode = compilationUnit.Generator.GreaterThanExpression(currentNode, right);
                                    break;
                            }
                        }
                        break;
                }
            }

            return currentNode;
        }

        private SyntaxNode WalkBaseValueStatementNode(IParseTree child) {
            switch (child) {
                case VisualBasic6Parser.ImplicitCallStmt_InStmtContext childCtx: {
                        return VisitImplicitCallStmt_InStmt(childCtx).FirstOrDefault();
                    }
                case VisualBasic6Parser.VsICSContext childCtx: {
                        return VisitImplicitCallStmt_InStmt(childCtx.implicitCallStmt_InStmt()).FirstOrDefault();
                    }
                case VisualBasic6Parser.VsLiteralContext childCtx: {
                        return compilationUnit.Generator.GenerateNodeForLiteralOrName(childCtx.GetText());
                    }
                case VisualBasic6Parser.LiteralContext childCtx: {
                        return compilationUnit.Generator.GenerateNodeForLiteralOrName(childCtx.GetText());
                    }
                case VisualBasic6Parser.ValueStmtContext childCtx: {
                        return WalkValueStmt(childCtx);
                    }
            }

            return null;
        }

        private SyntaxNode WalkVariableOrProcedureCall(VisualBasic6Parser.ICS_S_VariableOrProcedureCallContext context) {
            var identifier = context.GetChild<VisualBasic6Parser.AmbiguousIdentifierContext>(0).GetText();

            return compilationUnit.Generator.IdentifierName(identifier);
        }

        private (SyntaxNode finalType, SyntaxNode baseType) WalkTypeNode(VisualBasic6Parser.AsTypeClauseContext context, bool isArray) {
            if (context == null) {
                // This is an unspecified type. Treat it as an object
                var finalType = compilationUnit.Generator.TypeExpression(SpecialType.System_Object);
                return (finalType, finalType);
            }

            var tempTypeNode = VisitAsTypeClause(context).First();

            if (isArray) {
                return (compilationUnit.Generator.ArrayTypeExpression(tempTypeNode), tempTypeNode);
            } else {
                return (tempTypeNode, tempTypeNode);
            }
        }

        public override IEnumerable<SyntaxNode> VisitModuleHeader([NotNull] VisualBasic6Parser.ModuleHeaderContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitModuleConfig([NotNull] VisualBasic6Parser.ModuleConfigContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitModuleConfigElement([NotNull] VisualBasic6Parser.ModuleConfigElementContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitModuleAttributes([NotNull] VisualBasic6Parser.ModuleAttributesContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitModuleOptions([NotNull] VisualBasic6Parser.ModuleOptionsContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOptionBaseStmt([NotNull] VisualBasic6Parser.OptionBaseStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOptionCompareStmt([NotNull] VisualBasic6Parser.OptionCompareStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOptionExplicitStmt([NotNull] VisualBasic6Parser.OptionExplicitStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOptionPrivateModuleStmt([NotNull] VisualBasic6Parser.OptionPrivateModuleStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitModuleBody([NotNull] VisualBasic6Parser.ModuleBodyContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitAttributeStmt([NotNull] VisualBasic6Parser.AttributeStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitBlock([NotNull] VisualBasic6Parser.BlockContext context) {
            foreach (var childNode in EnumerateChildElements(context)) {
                yield return childNode;
            }
        }

        public override IEnumerable<SyntaxNode> VisitAppactivateStmt([NotNull] VisualBasic6Parser.AppactivateStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitBeepStmt([NotNull] VisualBasic6Parser.BeepStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitChdirStmt([NotNull] VisualBasic6Parser.ChdirStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitChdriveStmt([NotNull] VisualBasic6Parser.ChdriveStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitCloseStmt([NotNull] VisualBasic6Parser.CloseStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitConstSubStmt([NotNull] VisualBasic6Parser.ConstSubStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitDateStmt([NotNull] VisualBasic6Parser.DateStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitDeclareStmt([NotNull] VisualBasic6Parser.DeclareStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitDeftypeStmt([NotNull] VisualBasic6Parser.DeftypeStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitDeleteSettingStmt([NotNull] VisualBasic6Parser.DeleteSettingStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitDoLoopStmt([NotNull] VisualBasic6Parser.DoLoopStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitEndStmt([NotNull] VisualBasic6Parser.EndStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitEnumerationStmt([NotNull] VisualBasic6Parser.EnumerationStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitEnumerationStmt_Constant([NotNull] VisualBasic6Parser.EnumerationStmt_ConstantContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitEraseStmt([NotNull] VisualBasic6Parser.EraseStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitErrorStmt([NotNull] VisualBasic6Parser.ErrorStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitEventStmt([NotNull] VisualBasic6Parser.EventStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitExitStmt([NotNull] VisualBasic6Parser.ExitStmtContext context) {
            if (context.EXIT_SUB() != null) {
                yield return compilationUnit.Generator.ReturnStatement();
            }
        }

        public override IEnumerable<SyntaxNode> VisitFilecopyStmt([NotNull] VisualBasic6Parser.FilecopyStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitForEachStmt([NotNull] VisualBasic6Parser.ForEachStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitForNextStmt([NotNull] VisualBasic6Parser.ForNextStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitFunctionStmt([NotNull] VisualBasic6Parser.FunctionStmtContext context) {
            var accessibility = DetermineAccessibility(context.GetChild<VisualBasic6Parser.VisibilityContext>(0));

            var nameContext = context.GetChild<VisualBasic6Parser.AmbiguousIdentifierContext>(0);
            var name = nameContext.GetText();

            var methodContext = new MethodContext()
            {
                Accessibility = accessibility,
                Name = name
            };

            var blockContext = context.GetChild<VisualBasic6Parser.BlockContext>(0);
            if (blockContext == null) {
                // This is an empty method, skip it
                yield break;
            }

            compilationUnit.BlockCount++;

            var statements = EnumerateChildElements(blockContext);

            IEnumerable<SyntaxNode> argList = null;

            var argListContext = context.GetChild<VisualBasic6Parser.ArgListContext>(0);
            if (argListContext != null) {
                argList = VisitArgList(argListContext);
            }

            SyntaxNode returnTypeNode;

            var asTypeContext = context.GetChild<VisualBasic6Parser.AsTypeClauseContext>(0);
            if (asTypeContext != null) {
                returnTypeNode = VisitAsTypeClause(asTypeContext).FirstOrDefault();
            } else {
                returnTypeNode = compilationUnit.Generator.GenerateTypeNode("object");
            }

            var methodDeclaration = compilationUnit.Generator.MethodDeclaration(name, parameters: argList, returnType: returnTypeNode, accessibility: accessibility, statements: statements);

            compilationUnit.BlockCount--;

            yield return methodDeclaration;
        }

        public override IEnumerable<SyntaxNode> VisitGetStmt([NotNull] VisualBasic6Parser.GetStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitGoSubStmt([NotNull] VisualBasic6Parser.GoSubStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitGoToStmt([NotNull] VisualBasic6Parser.GoToStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitBlockIfThenElse([NotNull] VisualBasic6Parser.BlockIfThenElseContext context) {
            var ifBlockStmt = context.GetChild<VisualBasic6Parser.IfBlockStmtContext>(0);
            var ifConditionStmt = ifBlockStmt.GetChild<VisualBasic6Parser.IfConditionStmtContext>(0);
            var valueStmt = ifConditionStmt.GetChild<VisualBasic6Parser.ValueStmtContext>(0);
            var ifConditionNode = WalkValueStmt(valueStmt);

            var blockStmt = ifBlockStmt.GetChild<VisualBasic6Parser.BlockContext>(0);
            IEnumerable<SyntaxNode> blockStmtNodes;
            if (blockStmt == null) {
                blockStmtNodes = Enumerable.Empty<SyntaxNode>();
            } else {
                blockStmtNodes = VisitBlock(blockStmt);
            }

            IEnumerable<SyntaxNode> elseBlockStmtNodes = null;
            var ifElseBlockStmt = context.GetChild<VisualBasic6Parser.IfElseBlockStmtContext>(0);
            if (ifElseBlockStmt != null) {
                var elseBlock = ifElseBlockStmt.GetChild<VisualBasic6Parser.BlockContext>(0);
                if (elseBlock != null) {
                    elseBlockStmtNodes = VisitBlock(elseBlock);
                }
            }

            var node = compilationUnit.Generator.IfStatement(ifConditionNode, blockStmtNodes, elseBlockStmtNodes);

            yield return node;
        }

        public override IEnumerable<SyntaxNode> VisitIfBlockStmt([NotNull] VisualBasic6Parser.IfBlockStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitIfConditionStmt([NotNull] VisualBasic6Parser.IfConditionStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitIfElseIfBlockStmt([NotNull] VisualBasic6Parser.IfElseIfBlockStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitIfElseBlockStmt([NotNull] VisualBasic6Parser.IfElseBlockStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitImplementsStmt([NotNull] VisualBasic6Parser.ImplementsStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitInputStmt([NotNull] VisualBasic6Parser.InputStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitKillStmt([NotNull] VisualBasic6Parser.KillStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitLetStmt([NotNull] VisualBasic6Parser.LetStmtContext context) {
            var implicitCallNode = VisitImplicitCallStmt_InStmt(context.GetChild<VisualBasic6Parser.ImplicitCallStmt_InStmtContext>(0)).FirstOrDefault();

            if (implicitCallNode != null) {
                if (context.EQ() != null) {
                    var valueNode = WalkValueStmt(context.GetChild<VisualBasic6Parser.ValueStmtContext>(0));

                    yield return compilationUnit.Generator.AssignmentStatement(implicitCallNode, valueNode);
                }
            }
        }

        public override IEnumerable<SyntaxNode> VisitLineInputStmt([NotNull] VisualBasic6Parser.LineInputStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitLoadStmt([NotNull] VisualBasic6Parser.LoadStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitLockStmt([NotNull] VisualBasic6Parser.LockStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitLsetStmt([NotNull] VisualBasic6Parser.LsetStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitMacroIfThenElseStmt([NotNull] VisualBasic6Parser.MacroIfThenElseStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitMacroIfBlockStmt([NotNull] VisualBasic6Parser.MacroIfBlockStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitMacroElseIfBlockStmt([NotNull] VisualBasic6Parser.MacroElseIfBlockStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitMacroElseBlockStmt([NotNull] VisualBasic6Parser.MacroElseBlockStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitMidStmt([NotNull] VisualBasic6Parser.MidStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitMkdirStmt([NotNull] VisualBasic6Parser.MkdirStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitNameStmt([NotNull] VisualBasic6Parser.NameStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOnErrorStmt([NotNull] VisualBasic6Parser.OnErrorStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOnGoToStmt([NotNull] VisualBasic6Parser.OnGoToStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOnGoSubStmt([NotNull] VisualBasic6Parser.OnGoSubStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOpenStmt([NotNull] VisualBasic6Parser.OpenStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOutputList([NotNull] VisualBasic6Parser.OutputListContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitOutputList_Expression([NotNull] VisualBasic6Parser.OutputList_ExpressionContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitPrintStmt([NotNull] VisualBasic6Parser.PrintStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitPropertyGetStmt([NotNull] VisualBasic6Parser.PropertyGetStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitPropertySetStmt([NotNull] VisualBasic6Parser.PropertySetStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitPropertyLetStmt([NotNull] VisualBasic6Parser.PropertyLetStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitPutStmt([NotNull] VisualBasic6Parser.PutStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitRaiseEventStmt([NotNull] VisualBasic6Parser.RaiseEventStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitRandomizeStmt([NotNull] VisualBasic6Parser.RandomizeStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitRedimStmt([NotNull] VisualBasic6Parser.RedimStmtContext context) {
            var redimSubContext = context.GetChild<VisualBasic6Parser.RedimSubStmtContext>(0);
            var subscriptNode = VisitSubscripts(redimSubContext.GetChild<VisualBasic6Parser.SubscriptsContext>(0)).FirstOrDefault();

            var redimSubStmt = VisitRedimSubStmt(redimSubContext).First();

            var preserve = context.PRESERVE();
            if (preserve != null) {
                yield return compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.IdentifierName("Array"), "Resize"), compilationUnit.Generator.Argument(RefKind.Ref, redimSubStmt), subscriptNode);
            } else {
                yield return compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.IdentifierName("EclipseSupport"), "Resize"), compilationUnit.Generator.Argument(RefKind.Ref, redimSubStmt), subscriptNode, compilationUnit.Generator.Argument("preserve", RefKind.None, compilationUnit.Generator.FalseLiteralExpression()));
            }
        }

        public override IEnumerable<SyntaxNode> VisitRedimSubStmt([NotNull] VisualBasic6Parser.RedimSubStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitResetStmt([NotNull] VisualBasic6Parser.ResetStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitResumeStmt([NotNull] VisualBasic6Parser.ResumeStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitReturnStmt([NotNull] VisualBasic6Parser.ReturnStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitRmdirStmt([NotNull] VisualBasic6Parser.RmdirStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitRsetStmt([NotNull] VisualBasic6Parser.RsetStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitSavepictureStmt([NotNull] VisualBasic6Parser.SavepictureStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitSaveSettingStmt([NotNull] VisualBasic6Parser.SaveSettingStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitSeekStmt([NotNull] VisualBasic6Parser.SeekStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitSelectCaseStmt([NotNull] VisualBasic6Parser.SelectCaseStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitSC_Case([NotNull] VisualBasic6Parser.SC_CaseContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitCaseCondElse([NotNull] VisualBasic6Parser.CaseCondElseContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitCaseCondIs([NotNull] VisualBasic6Parser.CaseCondIsContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitCaseCondValue([NotNull] VisualBasic6Parser.CaseCondValueContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitCaseCondTo([NotNull] VisualBasic6Parser.CaseCondToContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitSendkeysStmt([NotNull] VisualBasic6Parser.SendkeysStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitSetattrStmt([NotNull] VisualBasic6Parser.SetattrStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitSetStmt([NotNull] VisualBasic6Parser.SetStmtContext context) {
            // TODO: Don't use FirstOrDefault
            var implicitCallNode = VisitImplicitCallStmt_InStmt(context.GetChild<VisualBasic6Parser.ImplicitCallStmt_InStmtContext>(0)).FirstOrDefault();

            if (implicitCallNode != null) {
                var valueNode = WalkValueStmt(context.GetChild<VisualBasic6Parser.ValueStmtContext>(0));

                yield return compilationUnit.Generator.AssignmentStatement(implicitCallNode, valueNode);
            }
        }

        public override IEnumerable<SyntaxNode> VisitStopStmt([NotNull] VisualBasic6Parser.StopStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitTimeStmt([NotNull] VisualBasic6Parser.TimeStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitTypeStmt([NotNull] VisualBasic6Parser.TypeStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitTypeStmt_Element([NotNull] VisualBasic6Parser.TypeStmt_ElementContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitTypeOfStmt([NotNull] VisualBasic6Parser.TypeOfStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitUnloadStmt([NotNull] VisualBasic6Parser.UnloadStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitUnlockStmt([NotNull] VisualBasic6Parser.UnlockStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsStruct([NotNull] VisualBasic6Parser.VsStructContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsAdd([NotNull] VisualBasic6Parser.VsAddContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsLt([NotNull] VisualBasic6Parser.VsLtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsAddressOf([NotNull] VisualBasic6Parser.VsAddressOfContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsNew([NotNull] VisualBasic6Parser.VsNewContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsMult([NotNull] VisualBasic6Parser.VsMultContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsNegation([NotNull] VisualBasic6Parser.VsNegationContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsAssign([NotNull] VisualBasic6Parser.VsAssignContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsLike([NotNull] VisualBasic6Parser.VsLikeContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsDiv([NotNull] VisualBasic6Parser.VsDivContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsPlus([NotNull] VisualBasic6Parser.VsPlusContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsNot([NotNull] VisualBasic6Parser.VsNotContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsGeq([NotNull] VisualBasic6Parser.VsGeqContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsTypeOf([NotNull] VisualBasic6Parser.VsTypeOfContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsICS([NotNull] VisualBasic6Parser.VsICSContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsNeq([NotNull] VisualBasic6Parser.VsNeqContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsXor([NotNull] VisualBasic6Parser.VsXorContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsAnd([NotNull] VisualBasic6Parser.VsAndContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsLeq([NotNull] VisualBasic6Parser.VsLeqContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsPow([NotNull] VisualBasic6Parser.VsPowContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsIs([NotNull] VisualBasic6Parser.VsIsContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsMod([NotNull] VisualBasic6Parser.VsModContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsAmp([NotNull] VisualBasic6Parser.VsAmpContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsOr([NotNull] VisualBasic6Parser.VsOrContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsMinus([NotNull] VisualBasic6Parser.VsMinusContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsLiteral([NotNull] VisualBasic6Parser.VsLiteralContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsEqv([NotNull] VisualBasic6Parser.VsEqvContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsImp([NotNull] VisualBasic6Parser.VsImpContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsGt([NotNull] VisualBasic6Parser.VsGtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsEq([NotNull] VisualBasic6Parser.VsEqContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVsMid([NotNull] VisualBasic6Parser.VsMidContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVariableListStmt([NotNull] VisualBasic6Parser.VariableListStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVariableSubStmt([NotNull] VisualBasic6Parser.VariableSubStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitWhileWendStmt([NotNull] VisualBasic6Parser.WhileWendStmtContext context) {
            var valueStmtContext = context.GetChild<VisualBasic6Parser.ValueStmtContext>(0);

            var valueStmt = WalkValueStmt(valueStmtContext);

            var blockContext = context.GetChild<VisualBasic6Parser.BlockContext>(0);
            if (blockContext == null) {
                // This is an empty block, skip it
                yield break;
            }

            compilationUnit.BlockCount++;

            var statements = EnumerateChildElements(blockContext);

            compilationUnit.BlockCount--;

            yield return compilationUnit.Generator.WhileStatement(valueStmt, statements);
        }

        public override IEnumerable<SyntaxNode> VisitWidthStmt([NotNull] VisualBasic6Parser.WidthStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitWithStmt([NotNull] VisualBasic6Parser.WithStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitWriteStmt([NotNull] VisualBasic6Parser.WriteStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitExplicitCallStmt([NotNull] VisualBasic6Parser.ExplicitCallStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitECS_ProcedureCall([NotNull] VisualBasic6Parser.ECS_ProcedureCallContext context) {
            var identifierContext = context.GetChild<VisualBasic6Parser.AmbiguousIdentifierContext>(0);

            SyntaxNode[] arguments = Array.Empty<SyntaxNode>();
            var argsCallContext = context.GetChild<VisualBasic6Parser.ArgsCallContext>(0);
            if (argsCallContext != null) {
                arguments = VisitArgsCall(argsCallContext).ToArray();
            }

            yield return compilationUnit.Rewriter.RewriteProcedureCall(identifierContext.GetText(), arguments);
        }

        public override IEnumerable<SyntaxNode> VisitECS_MemberProcedureCall([NotNull] VisualBasic6Parser.ECS_MemberProcedureCallContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitImplicitCallStmt_InBlock([NotNull] VisualBasic6Parser.ImplicitCallStmt_InBlockContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitICS_B_ProcedureCall([NotNull] VisualBasic6Parser.ICS_B_ProcedureCallContext context) {
            var certainIdentifier = context.GetChild<VisualBasic6Parser.CertainIdentifierContext>(0);

            SyntaxNode[] arguments = Array.Empty<SyntaxNode>();
            var argsCallContext = context.GetChild<VisualBasic6Parser.ArgsCallContext>(0);
            if (argsCallContext != null) {
                arguments = VisitArgsCall(argsCallContext).ToArray();
            }

            yield return compilationUnit.Rewriter.RewriteProcedureCall(certainIdentifier.GetText(), arguments);
        }

        public override IEnumerable<SyntaxNode> VisitICS_B_MemberProcedureCall([NotNull] VisualBasic6Parser.ICS_B_MemberProcedureCallContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitImplicitCallStmt_InStmt([NotNull] VisualBasic6Parser.ImplicitCallStmt_InStmtContext context) {
            var membersCall = context.iCS_S_MembersCall();
            if (membersCall != null) {
               foreach (var result in VisitICS_S_MembersCall(membersCall)) {
                    yield return result;
                }
            }

            var variableOrProcedureCall2 = context.iCS_S_VariableOrProcedureCall();
            if (variableOrProcedureCall2 != null) {
                yield return WalkVariableOrProcedureCall(variableOrProcedureCall2);
            }

            var procedureOrArrayCallCtx = context.iCS_S_ProcedureOrArrayCall();
            if (procedureOrArrayCallCtx != null) {
                foreach (var result in VisitICS_S_ProcedureOrArrayCall(procedureOrArrayCallCtx)) {
                    yield return result;
                }
            }
        }

        public override IEnumerable<SyntaxNode> VisitICS_S_VariableOrProcedureCall([NotNull] VisualBasic6Parser.ICS_S_VariableOrProcedureCallContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitICS_S_ProcedureOrArrayCall([NotNull] VisualBasic6Parser.ICS_S_ProcedureOrArrayCallContext context) {
            var identifierCtx = context.GetChild<VisualBasic6Parser.AmbiguousIdentifierContext>(0);
            string identifier = null;

            if (identifierCtx != null) {
                identifier = identifierCtx.GetText();
            }

            // TODO: Type hint support?

            var argsCallCtx = context.GetChild<VisualBasic6Parser.ArgsCallContext>(0);
            IEnumerable<SyntaxNode> argNodes = Enumerable.Empty<SyntaxNode>();
            if (argsCallCtx != null) {
                argNodes = VisitArgsCall(argsCallCtx);
            }

            yield return compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.IdentifierName(identifier), argNodes);
        }

        public override IEnumerable<SyntaxNode> VisitICS_S_MembersCall([NotNull] VisualBasic6Parser.ICS_S_MembersCallContext context) {
            SyntaxNode rootParent = null;

            var variableOrProcedureCall = context.iCS_S_VariableOrProcedureCall();
            if (variableOrProcedureCall != null) {
                rootParent = WalkVariableOrProcedureCall(context.iCS_S_VariableOrProcedureCall());
            }

            var procedureOrArrayCallContext = context.iCS_S_ProcedureOrArrayCall();
            if (procedureOrArrayCallContext != null) {
                foreach (var result in VisitICS_S_ProcedureOrArrayCall(procedureOrArrayCallContext)) {
                    rootParent = result;
                }
            }

            if (rootParent == null) {
                rootParent = compilationUnit.Generator.ThisExpression();
            }

            var memberCallCollection = context.iCS_S_MemberCall();

            foreach (var memberCall in memberCallCollection) {
                var memberCallVariableOrProcedureCall = memberCall.iCS_S_VariableOrProcedureCall();
                if (memberCallVariableOrProcedureCall != null) {
                    var memberCallVariableOrProcedureCallNode = WalkVariableOrProcedureCall(memberCallVariableOrProcedureCall);

                    rootParent = compilationUnit.Generator.MemberAccessExpression(rootParent, memberCallVariableOrProcedureCallNode);
                }
            }

            yield return rootParent;
        }

        public override IEnumerable<SyntaxNode> VisitICS_S_MemberCall([NotNull] VisualBasic6Parser.ICS_S_MemberCallContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitICS_S_DictionaryCall([NotNull] VisualBasic6Parser.ICS_S_DictionaryCallContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitArgsCall([NotNull] VisualBasic6Parser.ArgsCallContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitArgCall([NotNull] VisualBasic6Parser.ArgCallContext context) {
            if (context.BYREF() != null) {
                Debug.WriteLine("BYREF not supported");
            }

            if (context.PARAMARRAY() != null) {
                Debug.WriteLine("PARAMARRAY not supported");
            }

            yield return WalkValueStmt(context.GetChild<VisualBasic6Parser.ValueStmtContext>(0));
        }

        public override IEnumerable<SyntaxNode> VisitDictionaryCallStmt([NotNull] VisualBasic6Parser.DictionaryCallStmtContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitArgList([NotNull] VisualBasic6Parser.ArgListContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitArg([NotNull] VisualBasic6Parser.ArgContext context) {
            SyntaxNode initializer = null;

            if (context.OPTIONAL() != null) {
                initializer = VisitArgDefaultValue(context.GetChild<VisualBasic6Parser.ArgDefaultValueContext>(0)).First();
            }

            if (context.BYREF() != null) {
                Debug.WriteLine("BYREF not yet supported");
            }

            if (context.PARAMARRAY() != null) {
                Debug.WriteLine("PARAMARRAY not yet supported");
            }

            var paramName = context.GetChild<VisualBasic6Parser.AmbiguousIdentifierContext>(0).GetText();

            bool isArray = false;
            if (context.LPAREN() != null && context.RPAREN() != null) {
                isArray = true;
            }

            var (finalTypeNode, baseTypeNode) = WalkTypeNode(context.GetChild<VisualBasic6Parser.AsTypeClauseContext>(0), isArray);

            yield return compilationUnit.Generator.ParameterDeclaration(paramName, finalTypeNode, initializer, RefKind.None);
        }

        public override IEnumerable<SyntaxNode> VisitArgDefaultValue([NotNull] VisualBasic6Parser.ArgDefaultValueContext context) {
            var literal = context.literal();
            if (literal != null) {
                yield return compilationUnit.Generator.GenerateNodeForLiteral(literal.GetText());
            }

            var ambiguousIdentifier = context.ambiguousIdentifier();
            if (ambiguousIdentifier != null) {
                yield return compilationUnit.Generator.IdentifierName(ambiguousIdentifier.GetText());
            }
        }

        public override IEnumerable<SyntaxNode> VisitSubscripts([NotNull] VisualBasic6Parser.SubscriptsContext context) {
            if (context == null) {
                // No subscripts available
            } else {
                foreach (var subscript in EnumerateContexts<VisualBasic6Parser.SubscriptContext>(context)) {
                    foreach (var returnValue in VisitSubscript(subscript)) {
                        yield return returnValue;
                    }
                }
            }
        }

        public override IEnumerable<SyntaxNode> VisitSubscript([NotNull] VisualBasic6Parser.SubscriptContext context) {
            var to = context.TO();

            if (to == null) {
                // Uses a single subscript
                var subscriptValue = context.GetChild<VisualBasic6Parser.ValueStmtContext>(0).GetText();

                yield return compilationUnit.Generator.GenerateNodeForLiteralOrName(subscriptValue);
            } else {
                // Uses a subscript range
                // VB6 supports non-0 indexed arrays. .NET does not. To work around this, pad the start of the array with empty items, equal to the offset
                var subscriptValue1 = context.GetChild<VisualBasic6Parser.ValueStmtContext>(0).GetText();
                var subscriptValue2 = context.GetChild<VisualBasic6Parser.ValueStmtContext>(1).GetText();

                yield return compilationUnit.Generator.AddExpression(compilationUnit.Generator.GenerateNodeForLiteralOrName(subscriptValue1), compilationUnit.Generator.GenerateNodeForLiteralOrName(subscriptValue2));
            }
        }

        public override IEnumerable<SyntaxNode> VisitAmbiguousIdentifier([NotNull] VisualBasic6Parser.AmbiguousIdentifierContext context) {
            // DO NOT EDIT, ONLY USE FROM HIGHER LEVEL BLOCKS
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitAsTypeClause([NotNull] VisualBasic6Parser.AsTypeClauseContext context) {
            var typeStatement = context.GetChild<VisualBasic6Parser.TypeContext>(0);
            var baseTypeStatement = typeStatement.GetChild<VisualBasic6Parser.BaseTypeContext>(0);
            var complexTypeStatement = typeStatement.GetChild<VisualBasic6Parser.ComplexTypeContext>(0);

            string finalTypeName = "";

            if (baseTypeStatement != null) {
                finalTypeName = baseTypeStatement.GetText();
            }

            if (string.IsNullOrEmpty(finalTypeName)) {
                finalTypeName = "object";
                Debug.WriteLine("Invalid type node");
            }

            yield return compilationUnit.Generator.GenerateTypeNode(finalTypeName);
        }

        public override IEnumerable<SyntaxNode> VisitBaseType([NotNull] VisualBasic6Parser.BaseTypeContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitCertainIdentifier([NotNull] VisualBasic6Parser.CertainIdentifierContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitComparisonOperator([NotNull] VisualBasic6Parser.ComparisonOperatorContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitComplexType([NotNull] VisualBasic6Parser.ComplexTypeContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitFieldLength([NotNull] VisualBasic6Parser.FieldLengthContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitLetterrange([NotNull] VisualBasic6Parser.LetterrangeContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitLineLabel([NotNull] VisualBasic6Parser.LineLabelContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitLiteral([NotNull] VisualBasic6Parser.LiteralContext context) {
            // DO NOT EDIT, ONLY USE FROM HIGHER LEVEL BLOCKS
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitType([NotNull] VisualBasic6Parser.TypeContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitTypeHint([NotNull] VisualBasic6Parser.TypeHintContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitVisibility([NotNull] VisualBasic6Parser.VisibilityContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitAmbiguousKeyword([NotNull] VisualBasic6Parser.AmbiguousKeywordContext context) {
            return EnumerateChildElements(context);
        }

        public override IEnumerable<SyntaxNode> VisitTerminal(ITerminalNode node) {
            return EnumerateChildElements(node);
        }

        public override IEnumerable<SyntaxNode> VisitErrorNode(IErrorNode node) {
            return EnumerateChildElements(node);
        }

        public override IEnumerable<SyntaxNode> VisitChildren(IRuleNode node) {
            return EnumerateChildElements(node);
        }
    }
}
