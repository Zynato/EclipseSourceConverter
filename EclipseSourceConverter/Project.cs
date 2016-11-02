using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class Project
    {
        public List<ProjectItem> Items { get; }
        public List<ConvertedProjectItem> ConvertedItems { get; }

        public string Title { get; set; }
        public string ProjectNamespace { get; set; }
        public string ExecutableName { get; set; }
        public string CompanyName { get; set; }
        public string ProjectDirectory { get; set; }

        public Project() {
            this.Items = new List<ProjectItem>();
            this.ConvertedItems = new List<ConvertedProjectItem>();
        }
    }
}
