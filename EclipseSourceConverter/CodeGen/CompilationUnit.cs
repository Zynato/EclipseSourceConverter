using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter.CodeGen
{
    class CompilationUnit
    {
        public List<SyntaxNode> UsingDirectives { get; }
        public List<SyntaxNode> ContentDeclarations { get; }

        public List<SyntaxNode> Members { get; }

        public bool IsParentAClass {
            get {
                return BlockCount == 1;
            }
        }

        public int BlockCount { get; set; }

        public SyntaxGenerator Generator { get; }

        public MethodContext ActiveMethodContext { get; set; }

        public Rewriter Rewriter { get; }

        public SyntaxNode BaseType { get; set; }

        public CompilationUnit(SyntaxGenerator generator) {
            this.Generator = generator;

            this.UsingDirectives = new List<SyntaxNode>();
            this.ContentDeclarations = new List<SyntaxNode>();
            this.Members = new List<SyntaxNode>();
            this.Rewriter = new Rewriter(this);
        }

        public void AddUsing(string namespaceDirective) {
            foreach (var existingDirective in UsingDirectives) {
                if (existingDirective is UsingDirectiveSyntax existingUsingDirective) {
                    var name = existingUsingDirective.Name.GetText().ToString();
                    if (name == namespaceDirective) {
                        return;
                    }
                }
            }

            UsingDirectives.Add(Generator.NamespaceImportDeclaration(namespaceDirective));
        }

        public string Generate(string name) {
            var classDefinition = Generator.ClassDeclaration(
                  name, typeParameters: null,
                  accessibility: Accessibility.Public,
                  modifiers: DeclarationModifiers.Partial,
                  baseType: BaseType,
                  members: Members);
            ContentDeclarations.Add(classDefinition);

            var compilationDeclarations = UsingDirectives.Union(ContentDeclarations).ToList();

            return Generator.CompilationUnit(compilationDeclarations).NormalizeWhitespace().ToFullString();
        }
    }
}
