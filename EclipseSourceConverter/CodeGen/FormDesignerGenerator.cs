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
        PropertyMappingCollection mappings;
        TypeMappingCollection typeMappings;

        public FormDesignerGenerator(CompilationUnit compilationUnit, PropertyMappingCollection mappings, TypeMappingCollection typeMappings) {
            this.compilationUnit = compilationUnit;
            this.mappings = mappings;
            this.typeMappings = typeMappings;
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

            // Generate control field declarations
            foreach (var child in WalkFormObjects(form.Children)) {
                var targetType = typeMappings.GetMapping(child.Type);

                if (targetType.Type != null) {
                    compilationUnit.Members.Add(compilationUnit.Generator.FieldDeclaration(child.Name, targetType.Type, Accessibility.Private));
                }
            }

            return compilationUnit.Generate(form.Name);
        }

        private SyntaxNode GenerateInitializeComponentMethod(VB6Form form) {
            var statements = new List<SyntaxNode>();

            // Initialize individual controls
            foreach (var child in WalkFormObjects(form.Children)) {
                var targetType = typeMappings.GetMapping(child.Type);

                if (targetType.Type != null) {
                    statements.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), child.Name), compilationUnit.Generator.ObjectCreationExpression(targetType.Type)));
                    if (targetType.IsContainer) {
                        statements.Add(compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), child.Name), "SuspendLayout")));
                    }
                }
            }

            statements.Add(compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), "SuspendLayout")));

            statements.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), "components"), compilationUnit.Generator.ObjectCreationExpression(compilationUnit.Generator.IdentifierName("System.ComponentModel.Container"))));
            statements.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), "AutoScaleMode"), compilationUnit.Generator.IdentifierName("System.Windows.Forms.AutoScaleMode.Font")));

            statements.AddRange(GenerateControlProperties(form));

            // Finalize individual control initialization
            foreach (var child in WalkFormObjects(form.Children)) {
                var targetType = typeMappings.GetMapping(child.Type);

                if (targetType.Type != null) {
                    if (targetType.IsContainer) {
                        statements.Add(compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), child.Name), "ResumeLayout"), compilationUnit.Generator.FalseLiteralExpression()));
                        statements.Add(compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), child.Name), "PerformLayout")));
                    }
                }
            }

            statements.Add(compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), "ResumeLayout"), compilationUnit.Generator.FalseLiteralExpression()));

            return compilationUnit.Generator.MethodDeclaration("InitializeComponent", accessibility: Accessibility.Private, statements: statements);
        }

        private IEnumerable<IVB6FormObject> WalkFormObjects(IList<IVB6FormObject> objects) {
            foreach (var child in objects) {
                yield return child;

                foreach (var childChild in WalkFormObjects(child.Children)) {
                    yield return childChild;
                }
            }
        }

        private IList<SyntaxNode> GenerateControlProperties(IVB6FormObject control) {
            var propertyNodes = new List<SyntaxNode>();

            int? height = null;
            int? width = null;
            int? top = null;
            int? left = null;

            foreach (var property in control.Properties) {
                if (property.Name == "Height") {
                    height = Convert.ToInt32(property.Value);
                } else if (property.Name == "Width") {
                    width = Convert.ToInt32(property.Value);
                } else if (property.Name == "Top") {
                    top = Convert.ToInt32(property.Value);
                } else if (property.Name == "Left") {
                    left = Convert.ToInt32(property.Value);
                } else {
                    var mapping = mappings.GetMapping(control, property.Name);
                    if (mapping != null) {
                        foreach (var mappingResult in mapping.Mapping(property.Value)) {
                            if (mappingResult.Value != null) {
                                if (control.Type == "Form") {
                                    propertyNodes.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), mappingResult.Property), mappingResult.Value));
                                } else {
                                    propertyNodes.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), control.Name), mappingResult.Property), mappingResult.Value));
                                }
                            }
                        }
                    }
                }
            }

            if (height.HasValue && width.HasValue) {
                width = UnitConverter.ConvertTwipsToXPixels(width.Value);
                height = UnitConverter.ConvertTwipsToYPixels(height.Value);

                var result = compilationUnit.Generator.ObjectCreationExpression(compilationUnit.Generator.IdentifierName("System.Drawing.Size"), compilationUnit.Generator.LiteralExpression(width.Value), compilationUnit.Generator.LiteralExpression(height.Value));

                propertyNodes.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), control.Name), "Size"), result));
            }

            if (top.HasValue && left.HasValue) {
                left = UnitConverter.ConvertTwipsToXPixels(left.Value);
                top = UnitConverter.ConvertTwipsToYPixels(top.Value);

                var result = compilationUnit.Generator.ObjectCreationExpression(compilationUnit.Generator.IdentifierName("System.Drawing.Point"), compilationUnit.Generator.LiteralExpression(left.Value), compilationUnit.Generator.LiteralExpression(top.Value));

                propertyNodes.Add(compilationUnit.Generator.AssignmentStatement(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), control.Name), "Location"), result));
            }

            // Add children
            foreach (var childControl in control.Children) {
                if (control.Type == "Form") {
                    propertyNodes.Add(compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), "Controls"), "Add"), compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), childControl.Name)));
                } else {
                    propertyNodes.Add(compilationUnit.Generator.InvocationExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), control.Name), "Controls"), "Add"), compilationUnit.Generator.MemberAccessExpression(compilationUnit.Generator.ThisExpression(), childControl.Name)));
                }
            }

            foreach (var childControl in control.Children) {
                propertyNodes.AddRange(GenerateControlProperties(childControl));
            }

            return propertyNodes;
        }
    }
}
