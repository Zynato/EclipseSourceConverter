using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class VB6ProjectLoader
    {
        public static Project LoadProject(string projectFile) {
            var project = new Project();

            var basePath = Path.GetDirectoryName(projectFile);

            using (var inputStream = new FileStream(projectFile, FileMode.Open)) {
                using (var streamReader = new StreamReader(inputStream)) {
                    while (!streamReader.EndOfStream) {
                        var line = streamReader.ReadLine();

                        var kvp = line.Split('=');
                        switch (kvp[0]) {
                            case "Module": {
                                    var moduleKvp = kvp[1].Split(';');
                                    var moduleName = moduleKvp[0];
                                    var modulePath = Path.Combine(basePath, moduleKvp[1].Trim());

                                    project.Items.Add(new ProjectItem(ProjectItemType.Module, moduleName, modulePath));
                                }
                                break;
                            case "Class": {
                                    var classKvp = kvp[1].Split(';');
                                    var className = classKvp[0];
                                    var classPath = Path.Combine(basePath, classKvp[1].Trim());

                                    project.Items.Add(new ProjectItem(ProjectItemType.Class, className, classPath));
                                }
                                break;
                            case "Form": {
                                    var formPath = Path.Combine(basePath, kvp[1]);
                                    var formName = Path.GetFileNameWithoutExtension(formPath);

                                    project.Items.Add(new ProjectItem(ProjectItemType.Form, formName, formPath));
                                }
                                break;
                            case "Title": {
                                    project.Title = kvp[1].Trim('\"');
                                }
                                break;
                            case "ExeName32": {
                                    project.ExecutableName = kvp[1].Trim('\"');
                                }
                                break;
                            case "VersionCompanyName": {
                                    project.CompanyName = kvp[1].Trim('\"');
                                }
                                break;
                        }
                    }
                }
            }

            return project;
        }
    }
}
