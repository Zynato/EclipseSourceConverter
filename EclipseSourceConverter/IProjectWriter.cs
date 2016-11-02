using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    interface IProjectWriter
    {
        void WriteProjectFile(string targetDirectory, Project project);
    }
}
