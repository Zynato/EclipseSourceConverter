using Antlr4.Runtime;
using EclipseSourceConverter.CodeGen;
using EclipseSourceConverter.VB6;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class ProjectConverter
    {
        public void ConvertProject(Project project, CodeGenLanguage language) {
            foreach (var item in project.Items) {
                if (item.Type == ProjectItemType.Module) {
                    ConvertCodeFile(item.SourceFile, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ESC", item.Name + ".cs"), item.Name, language);
                }
            }
        }

        private void ConvertCodeFile(string inputPath, string outputPath, string name, CodeGenLanguage language) {
            // TODO: [HACK] [TODO] Currently only known supported files are allowed
            if (name != "modGlobals" && name != "modConstants" && name != "modGeneral" /*&& name != "modDatabase" && name != "modGameEditors"*/) {
                return;
            }

            var workspace = new AdhocWorkspace();
            var generator = GetGenerator(workspace, language);

            var compilationUnit = new CompilationUnit(generator);

            var istream = new FileStream(inputPath, FileMode.Open);

            var input = new AntlrInputStream(istream);
            var lexer = new VisualBasic6Lexer(input);
            var tokens = new CommonTokenStream(lexer);
            var parser = new VisualBasic6Parser(tokens);
            var tree = parser.startRule();

            var visitor = new ESCVisitor(name, compilationUnit);
            foreach (var node in visitor.Visit(tree)) {
                // Just enumerate it all
            }

            var result = compilationUnit.Generate(name);

            File.WriteAllText(outputPath, result);
        }

        private SyntaxGenerator GetGenerator(Workspace workspace, CodeGenLanguage language) {
            switch (language) {
                case CodeGenLanguage.CSharp:
                    return SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
                case CodeGenLanguage.VB:
                    return SyntaxGenerator.GetGenerator(workspace, LanguageNames.VisualBasic);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
