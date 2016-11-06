using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class TypeMappingCollection
    {
        Dictionary<string, Func<TypeMapping>> typeMappings;

        public TypeMappingCollection() {
            this.typeMappings = new Dictionary<string, Func<TypeMapping>>();
        }

        public void RegisterTypeMapping(string type, Func<TypeMapping> mappedType) {
            this.typeMappings.Add(type, mappedType);
        }

        public TypeMapping GetMapping(string type) {
            if (typeMappings.TryGetValue(type, out var mapping)) {
                return mapping();
            }

            return default(TypeMapping);
        }
    }
}
