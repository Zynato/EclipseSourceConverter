using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter.CodeGen
{
    static class CodeGenLanguageExtensions
    {
        public static string GetLanguageExtension(this CodeGenLanguage language) {
            switch (language) {
                case CodeGenLanguage.CSharp:
                    return ".cs";
                case CodeGenLanguage.VB:
                    return ".vb";
                default:
                    throw new NotSupportedException();
            }
        }

        public static IProjectWriter GetProjectWriter(this CodeGenLanguage language) {
            switch (language) {
                case CodeGenLanguage.CSharp:
                    return new CSProjProjectWriter();
                case CodeGenLanguage.VB:
                    return new VBProjProjectWriter();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
