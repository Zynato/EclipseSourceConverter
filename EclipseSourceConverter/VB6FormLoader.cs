using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    static class VB6FormLoader
    {
        public static VB6Form LoadForm(string formPath) {
            VB6Form form;

            using (var inputStream = new FileStream(formPath, FileMode.Open)) {
                using (var streamReader = new StreamReader(inputStream)) {
                    streamReader.ReadLine(); // Version header

                    form = (VB6Form)LoadFormObject(streamReader);

                    // All properties loaded, rest is code-behind
                    form.CodeBehind = streamReader.ReadToEnd();
                }
            }

            return form;
        }

        private static IVB6FormObject LoadFormObject(StreamReader streamReader) {
            var line = streamReader.ReadLine();
            while (!line.StartsWith("Begin ")) {
                line = streamReader.ReadLine();
            }

            var header = line.Trim().Split(' ');

            if (header[0] != "Begin") {
                throw new InvalidOperationException("Invalid header!");
            }

            var formObject = GenerateFormObject(header);

            ReadProperties(streamReader, formObject, formObject.Properties);

            return formObject;
        }

        private static IVB6FormObject GenerateFormObject(string[] headerArgs) {
            IVB6FormObject formObject;

            var objectType = headerArgs[1].Substring("VB.".Length);
            var objectName = headerArgs[2];

            if (objectType == "Form") {
                formObject = new VB6Form();
            } else {
                formObject = new VB6FormControl(objectType);
            }

            formObject.Name = objectName;

            return formObject;
        }

        private static void ReadProperties(StreamReader streamReader, IVB6FormObject parentObject, List<VB6FormControlProperty> properties) {
            while (true) {
                var line = streamReader.ReadLine().Trim();

                if (line.StartsWith("BeginProperty ")) {
                    var args = line.Split(' ');

                    var property = new VB6FormControlProperty();
                    property.Name = args[1];

                    ReadProperties(streamReader, parentObject, property.ChildProperties);

                    properties.Add(property);
                } else if (line.StartsWith("Begin ")) {
                    var args = line.Split(' ');

                    var childObject = GenerateFormObject(args);
                    parentObject.Children.Add(childObject);

                    ReadProperties(streamReader, childObject, childObject.Properties);
                } else if (line  == "EndProperty") {
                    return;
                } else if (line == "End") {
                    return;
                } else {
                    // Regular property, add it
                    var args = line.Split('=');

                    properties.Add(new VB6FormControlProperty() { Name = args[0].Trim(), Value = args[1].Trim() });
                }
            }
        }
    }
}
