using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class PropertyMappingCollection
    {
        Dictionary<string, PropertyMapping> generalMappings;
        Dictionary<string, Dictionary<string, PropertyMapping>> specificMappings;

        public PropertyMappingCollection() {
            this.generalMappings = new Dictionary<string, PropertyMapping>();
            this.specificMappings = new Dictionary<string, Dictionary<string, PropertyMapping>>();
        }

        public PropertyMapping GetMapping(IVB6FormObject formObject, string property) {
            if (specificMappings.TryGetValue(formObject.Type, out var specificMapping)) {
                if (specificMapping.TryGetValue(property, out var mapping)) {
                    return mapping;
                }
            }

            if (generalMappings.TryGetValue(property, out var generalMapping)) {
                return generalMapping;
            }

            return null;
        }

        public void RegisterGeneralMapping(string property, PropertyMapping mapping) {
            generalMappings.Add(property, mapping);
        }
    }
}
