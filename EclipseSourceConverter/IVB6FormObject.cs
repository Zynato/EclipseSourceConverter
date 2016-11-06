using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    interface IVB6FormObject
    {
        List<VB6FormControlProperty> Properties { get; }
        List<IVB6FormObject> Children { get; }
        string Name { get; set; }
        string Type { get; }
    }
}
