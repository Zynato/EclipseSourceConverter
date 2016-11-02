using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class ConvertedProjectItem
    {
        public ProjectItem Item { get; }
        public string DestinationPath { get; }

        public ConvertedProjectItem(ProjectItem item, string destinationPath) {
            this.Item = item;
            this.DestinationPath = destinationPath;
        }
    }
}
