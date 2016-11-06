using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    struct PropertyMappingResult
    {
        public readonly string Property;
        public readonly SyntaxNode Value;

        public PropertyMappingResult(string property, SyntaxNode value) {
            this.Property = property;
            this.Value = value;
        }
    }
}
