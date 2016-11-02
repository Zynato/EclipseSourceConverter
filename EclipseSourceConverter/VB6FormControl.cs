using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class VB6FormControl : IVB6FormObject
    {
        public List<VB6FormControlProperty> Properties { get; }
        public List<IVB6FormObject> Children { get; }
        public string Name { get; set; }

        public VB6FormControl() {
            this.Properties = new List<VB6FormControlProperty>();
            this.Children = new List<IVB6FormObject>();
        }
    }
}
