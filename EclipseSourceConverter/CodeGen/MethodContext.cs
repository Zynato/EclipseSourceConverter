using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter.CodeGen
{
    class MethodContext
    {
        public Accessibility Accessibility { get; set; }
        public string Name { get; set; }
    }
}
