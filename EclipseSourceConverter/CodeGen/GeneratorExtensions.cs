using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter.CodeGen
{
    static class GeneratorExtensions
    {
        public static SyntaxNode GenerateNodeForLiteral(this Microsoft.CodeAnalysis.Editing.SyntaxGenerator generator, string text) {
            if (text.Equals("Nothing")) {
                return generator.LiteralExpression(null);
            }

            if (text.Equals("vbNullString")) {
                return generator.LiteralExpression(null);
            }

            if (Int32.TryParse(text, out int i)) {
                return generator.LiteralExpression(i);
            }

            if (bool.TryParse(text, out bool b)) {
                return generator.LiteralExpression(b);
            }

            if (text.StartsWith("&H")) {
                if (text.EndsWith("&")) {
                    // This is a VB6 long hex literal (Int32 in .NET)
                    if (Int32.TryParse(text.Substring(2, text.Length - 1 - 2), NumberStyles.HexNumber, null, out i)) {
                        return generator.LiteralExpression(i);
                    }
                } else {
                    // This is a VB6 int hex literal (Int16 in .NET)
                    if (Int16.TryParse(text.Substring(2, text.Length - 2), NumberStyles.HexNumber, null, out short s)) {
                        return generator.LiteralExpression(s);
                    }
                }
            }

            if (text.StartsWith("\"") && text.EndsWith("\"")) {
                return generator.LiteralExpression(EscapeString(text.Trim('\"')));
            }

            return null;
        }

        public static SyntaxNode GenerateNodeForLiteralOrName(this Microsoft.CodeAnalysis.Editing.SyntaxGenerator generator, string text) {
            var literalNode = generator.GenerateNodeForLiteral(text);

            if (literalNode != null) {
                return literalNode;
            } else {
                return generator.IdentifierName(text);
            }
        }

        private static string EscapeString(string input) {
            return input.Replace("\\", "\\\\");
        }

        public static SyntaxNode GenerateTypeNode(this Microsoft.CodeAnalysis.Editing.SyntaxGenerator generator, string type) {
            switch (type.ToLower()) {
                case "long": // long in VB6 = Int32 in .NET
                    return generator.TypeExpression(SpecialType.System_Int32);
                case "string":
                    return generator.TypeExpression(SpecialType.System_String);
                case "boolean":
                    return generator.TypeExpression(SpecialType.System_Boolean);
                case "byte":
                    return generator.TypeExpression(SpecialType.System_Byte);
                case "single":
                    return generator.TypeExpression(SpecialType.System_Single);
                case "integer": // integer in VB6 = Int16 in .NET
                    return generator.TypeExpression(SpecialType.System_Int16);
                case "double":
                    return generator.TypeExpression(SpecialType.System_Double);
                case "object":
                    return generator.TypeExpression(SpecialType.System_Object);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
