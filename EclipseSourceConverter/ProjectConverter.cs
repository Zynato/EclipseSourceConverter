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
            string targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ESC");

            if (!Directory.Exists(targetDirectory)) {
                Directory.CreateDirectory(targetDirectory);
            }

            var convertedItems = new List<string>();
            foreach (var item in project.Items) {
                if (item.Type == ProjectItemType.Module) {
                    using (var inputStream = new FileStream(item.SourceFile, FileMode.Open)) {
                        var compilationUnit = BuildCompilationUnit(language);

                        if (ConvertCodeFile(inputStream, Path.Combine(targetDirectory, item.Name + language.GetLanguageExtension()), "Eclipse", compilationUnit)) {
                            project.ConvertedItems.Add(new ConvertedProjectItem(item, item.Name + language.GetLanguageExtension()));
                        }
                    }
                } else if (item.Type == ProjectItemType.Form) {
                    var form = VB6FormLoader.LoadForm(item.SourceFile);

                    var designerCompilationUnit = BuildCompilationUnit(language);
                    var propertyMappings = BuildPropertyMappings(designerCompilationUnit);
                    var typeMappings = BuildTypeMappings(designerCompilationUnit);
                    var formDesignerGenerator = new FormDesignerGenerator(designerCompilationUnit, propertyMappings, typeMappings);

                    var code = formDesignerGenerator.Generate(form);

                    var designerPath = Path.Combine(targetDirectory, $"{form.Name}.Designer{language.GetLanguageExtension()}");
                    project.ConvertedItems.Add(new ConvertedProjectItem(item, $"{form.Name}.Designer{language.GetLanguageExtension()}", $"{form.Name}{language.GetLanguageExtension()}"));
                    File.WriteAllText(designerPath, code);

                    // Generate code-behind
                    using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(form.CodeBehind))) {
                        var codeBehindCompilationUnit = BuildCompilationUnit(language);
                        codeBehindCompilationUnit.UsingDirectives.Add(codeBehindCompilationUnit.Generator.NamespaceImportDeclaration("System.Windows.Forms"));
                        codeBehindCompilationUnit.BaseType = codeBehindCompilationUnit.Generator.IdentifierName("Form");

                        codeBehindCompilationUnit.Members.Add(codeBehindCompilationUnit.Generator.ConstructorDeclaration(accessibility: Accessibility.Public, statements: new SyntaxNode[]
                        {
                            codeBehindCompilationUnit.Generator.InvocationExpression(codeBehindCompilationUnit.Generator.MemberAccessExpression(codeBehindCompilationUnit.Generator.ThisExpression(), "InitializeComponent"))
                        }));

                        if (ConvertCodeFile(inputStream, Path.Combine(targetDirectory, item.Name + language.GetLanguageExtension()), item.Name, codeBehindCompilationUnit)) {
                            project.ConvertedItems.Add(new ConvertedProjectItem(item, item.Name + language.GetLanguageExtension()));
                        }
                    }
                }
            }

            var projectWriter = language.GetProjectWriter();
            projectWriter.WriteProjectFile(targetDirectory, project);
        }

        private bool ConvertCodeFile(Stream inputStream, string outputPath, string name, CompilationUnit compilationUnit) {
            var input = new AntlrInputStream(inputStream);
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

            return true;
        }

        private CompilationUnit BuildCompilationUnit(CodeGenLanguage language) {
            var workspace = new AdhocWorkspace();
            var generator = GetGenerator(workspace, language);

            return new CompilationUnit(generator);
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

        private PropertyMappingCollection BuildPropertyMappings(CompilationUnit compilationUnit) {
            var mappings = new PropertyMappingCollection();

            mappings.RegisterGeneralMapping("Caption", new PropertyMapping(value => new PropertyMappingResult[] { new PropertyMappingResult("Text", compilationUnit.Generator.GenerateNodeForLiteral(value)) }));
            mappings.RegisterGeneralMapping("Name", new PropertyMapping(value => new PropertyMappingResult[] { new PropertyMappingResult("Name", compilationUnit.Generator.GenerateNodeForLiteral(value)) }));
            mappings.RegisterGeneralMapping("TabIndex", new PropertyMapping(value => new PropertyMappingResult[] { new PropertyMappingResult("TabIndex", compilationUnit.Generator.GenerateNodeForLiteral(value)) }));

            return mappings;
        }

        private TypeMappingCollection BuildTypeMappings(CompilationUnit compilationUnit) {
            var mappings = new TypeMappingCollection();

            mappings.RegisterTypeMapping("Label", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.Label"), false));
            mappings.RegisterTypeMapping("CommandButton", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.Button"), false));
            mappings.RegisterTypeMapping("Frame", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.GroupBox"), true));
            mappings.RegisterTypeMapping("ComboBox", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.ComboBox"), false));
            mappings.RegisterTypeMapping("HScrollBar", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.HScrollBar"), false));
            mappings.RegisterTypeMapping("VScrollBar", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.VScrollBar"), false));
            mappings.RegisterTypeMapping("TextBox", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.TextBox"), false));
            mappings.RegisterTypeMapping("PictureBox", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.PictureBox"), false));
            mappings.RegisterTypeMapping("ListBox", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.ListBox"), false));
            mappings.RegisterTypeMapping("CheckBox", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.CheckBox"), false));
            mappings.RegisterTypeMapping("OptionButton", () => new TypeMapping(compilationUnit.Generator.IdentifierName("System.Windows.Forms.RadioButton"), false));

            return mappings;
        }
    }
}
