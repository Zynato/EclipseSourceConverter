using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter.CodeGen
{
    class Rewriter
    {
        CompilationUnit compilationUnit;

        public Rewriter(CompilationUnit compilationUnit) {
            this.compilationUnit = compilationUnit;
        }

        public SyntaxNode RewriteProcedureCall(string identifier, SyntaxNode[] arguments) {
            switch (identifier) {
                case "MsgBox": {
                        compilationUnit.AddUsing("System.Windows.Forms");

                        var textArgument = arguments[0];

                        // MessageBox.Show([textArgument])
                        return compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.IdentifierName("MessageBox"), "Show"), textArgument);
                    }
                default: {
                        return compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.IdentifierName(identifier), arguments);
                    }
            }
        }
    }
}
