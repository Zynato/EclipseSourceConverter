using Antlr4.Runtime;
using EclipseSourceConverter.CodeGen;
using EclipseSourceConverter.VB6;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class Program
    {
        static void Main(string[] args) {
            var projectPath = Path.Combine("client", "client.vbp");

            var project = VB6ProjectLoader.LoadProject(projectPath);

            var projectConverter = new ProjectConverter();
            projectConverter.ConvertProject(project, CodeGenLanguage.CSharp);
        }
    }
}
