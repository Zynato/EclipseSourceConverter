using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class VB6FormControlProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public List<VB6FormControlProperty> ChildProperties { get; }

        public VB6FormControlProperty() {
            this.ChildProperties = new List<VB6FormControlProperty>();
        }
    }
}
