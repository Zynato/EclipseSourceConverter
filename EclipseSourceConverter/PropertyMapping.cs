using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class PropertyMapping
    {
        public Func<string, IEnumerable<PropertyMappingResult>> Mapping { get; }

        public PropertyMapping(Func<string, IEnumerable<PropertyMappingResult>> mapping) {
            this.Mapping = mapping;
        }
    }
}
