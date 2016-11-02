using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class ProjectItem
    {
        public ProjectItemType Type { get; }
        public string Name { get; }
        public string SourceFile { get; }

        public ProjectItem(ProjectItemType type, string name, string sourceFile) {
            this.Type = type;
            this.Name = name;
            this.SourceFile = sourceFile;
        }
    }
}
