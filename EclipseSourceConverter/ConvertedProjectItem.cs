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
        public string DependentUpon { get; }

        public ConvertedProjectItem(ProjectItem item, string destinationPath, string dependentUpon = null) {
            this.Item = item;
            this.DestinationPath = destinationPath;
            this.DependentUpon = dependentUpon;
        }
    }
}
