using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter.CodeGen
{
    class FormDesignerGenerator
    {
        CompilationUnit compilationUnit;

        public FormDesignerGenerator(CompilationUnit compilationUnit) {
            this.compilationUnit = compilationUnit;
        }

        public string Generate(VB6Form form) {
            // TODO: Generate appropriate comments
            // The following block generates this code:
            // private System.ComponentModel.IContainer components = null;
            compilationUnit.Members.Add(compilationUnit.Generator.FieldDeclaration("components",
                compilationUnit.Generator.IdentifierName("System.ComponentModel.IContainer"), Accessibility.Private, initializer: compilationUnit.Generator.NullLiteralExpression()));

            // The following block generates this code:
            //  protected override void Dispose(bool disposing) {
            //      if (disposing && (components != null)) {
            //          components.Dispose();
            //      }
            //      base.Dispose(disposing);
            //  }
            compilationUnit.Members.Add(compilationUnit.Generator.MethodDeclaration("Dispose",
                    new SyntaxNode[] { compilationUnit.Generator.ParameterDeclaration("disposing", compilationUnit.Generator.TypeExpression(SpecialType.System_Boolean)) },
                    accessibility: Accessibility.Protected,
                    modifiers: DeclarationModifiers.Override,
                    statements: new SyntaxNode[] {
                        compilationUnit.Generator.IfStatement(compilationUnit.Generator.LogicalAndExpression(compilationUnit.Generator.IdentifierName("disposing"), compilationUnit.Generator.ValueNotEqualsExpression(compilationUnit.Generator.IdentifierName("components"), compilationUnit.Generator.NullLiteralExpression())),
                                                              new SyntaxNode[] {  compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.IdentifierName("components"), "Dispose")) }),
                        compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.BaseExpression(), "Dispose"), compilationUnit.Generator.IdentifierName("disposing"))
                    }));

            compilationUnit.Members.Add(GenerateInitializeComponentMethod(form));

            return compilationUnit.Generate(form.Name);
        }

        private SyntaxNode GenerateInitializeComponentMethod(VB6Form form) {
            var statements = new List<SyntaxNode>();
            statements.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), "components"), compilationUnit.Generator.ObjectCreationExpression(compilationUnit.Generator.IdentifierName("System.ComponentModel.Container"))));
            statements.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), "AutoScaleMode"), compilationUnit.Generator.IdentifierName("System.Windows.Forms.AutoScaleMode.Font")));

            statements.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), "Text"), compilationUnit.Generator.LiteralExpression(form.Name)));

            return compilationUnit.Generator.MethodDeclaration("InitializeComponent", accessibility: Accessibility.Private, statements: statements);
        }
    }
}
