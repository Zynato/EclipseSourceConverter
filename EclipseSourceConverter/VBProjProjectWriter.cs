using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class VBProjProjectWriter : IProjectWriter 
    {
        public void WriteProjectFile(string targetDirectory, Project project) {
            using (var file = new FileStream(Path.Combine(targetDirectory, project.Title + ".vbproj"), FileMode.Create)) {
                using (var streamWriter = new StreamWriter(file)) {
                    streamWriter.WriteLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                    streamWriter.WriteLine(@"<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">");
                    streamWriter.WriteLine(@"  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />");
                    streamWriter.WriteLine(@"  <PropertyGroup>");
                    streamWriter.WriteLine(@"    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>");
                    streamWriter.WriteLine(@"    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>");
                    streamWriter.WriteLine(@"    <ProjectGuid>{0}</ProjectGuid>", Guid.NewGuid());
                    streamWriter.WriteLine(@"    <OutputType>Exe</OutputType>");
                    streamWriter.WriteLine(@"    <StartupObject>{0}.Program</StartupObject>", project.ProjectNamespace);
                    streamWriter.WriteLine(@"    <RootNamespace>{0}</RootNamespace>", project.ProjectNamespace);
                    streamWriter.WriteLine(@"    <AssemblyName>{0}</AssemblyName>", project.Title);
                    streamWriter.WriteLine(@"    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>");
                    streamWriter.WriteLine(@"    <FileAlignment>512</FileAlignment>");
                    streamWriter.WriteLine(@"    <MyType>Console</MyType>");
                    streamWriter.WriteLine(@"    <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>");
                    streamWriter.WriteLine(@"  </PropertyGroup>");
                    streamWriter.WriteLine(@"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">");
                    streamWriter.WriteLine(@"    <PlatformTarget>AnyCPU</PlatformTarget>");
                    streamWriter.WriteLine(@"    <DebugSymbols>true</DebugSymbols>");
                    streamWriter.WriteLine(@"    <DebugType>full</DebugType>");
                    streamWriter.WriteLine(@"    <Optimize>false</Optimize>");
                    streamWriter.WriteLine(@"    <OutputPath>bin\Debug\</OutputPath>");
                    streamWriter.WriteLine(@"    <DefineConstants>DEBUG;TRACE</DefineConstants>");
                    streamWriter.WriteLine(@"    <ErrorReport>prompt</ErrorReport>");
                    streamWriter.WriteLine(@"    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>");
                    streamWriter.WriteLine(@"  </PropertyGroup>");
                    streamWriter.WriteLine(@"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">");
                    streamWriter.WriteLine(@"    <PlatformTarget>AnyCPU</PlatformTarget>");
                    streamWriter.WriteLine(@"    <DebugType>pdbonly</DebugType>");
                    streamWriter.WriteLine(@"    <Optimize>true</Optimize>");
                    streamWriter.WriteLine(@"    <OutputPath>bin\Release\</OutputPath>");
                    streamWriter.WriteLine(@"    <DefineConstants>TRACE</DefineConstants>");
                    streamWriter.WriteLine(@"    <ErrorReport>prompt</ErrorReport>");
                    streamWriter.WriteLine(@"    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>");
                    streamWriter.WriteLine(@"  </PropertyGroup>");
                    streamWriter.WriteLine(@"  <PropertyGroup>");
                    streamWriter.WriteLine(@"    <OptionExplicit>On</OptionExplicit>");
                    streamWriter.WriteLine(@"  </PropertyGroup>");
                    streamWriter.WriteLine(@"  <PropertyGroup>");
                    streamWriter.WriteLine(@"    <OptionCompare>Binary</OptionCompare>");
                    streamWriter.WriteLine(@"  </PropertyGroup>");
                    streamWriter.WriteLine(@"  <PropertyGroup>");
                    streamWriter.WriteLine(@"    <OptionStrict>Off</OptionStrict>");
                    streamWriter.WriteLine(@"  </PropertyGroup>");
                    streamWriter.WriteLine(@"  <PropertyGroup>");
                    streamWriter.WriteLine(@"    <OptionInfer>On</OptionInfer>");
                    streamWriter.WriteLine(@"  </PropertyGroup>");
                    streamWriter.WriteLine(@"  <ItemGroup>");
                    streamWriter.WriteLine(@"    <Reference Include=""System"" />");
                    streamWriter.WriteLine(@"    <Reference Include=""System.Core"" />");
                    streamWriter.WriteLine(@"    <Reference Include=""System.Deployment"" />");
                    streamWriter.WriteLine(@"    <Reference Include=""System.Xml.Linq"" />");
                    streamWriter.WriteLine(@"    <Reference Include=""System.Data.DataSetExtensions"" />");
                    streamWriter.WriteLine(@"    <Reference Include=""System.Data"" />");
                    streamWriter.WriteLine(@"    <Reference Include=""System.Net.Http"" />");
                    streamWriter.WriteLine(@"    <Reference Include=""System.Xml"" />");
                    streamWriter.WriteLine(@"    <Reference Include=""System.Windows.Forms"" />");
                    streamWriter.WriteLine(@"  </ItemGroup>");
                    streamWriter.WriteLine(@"  <ItemGroup>");
                    streamWriter.WriteLine(@"    <Import Include=""Microsoft.VisualBasic"" />");
                    streamWriter.WriteLine(@"    <Import Include=""System"" />");
                    streamWriter.WriteLine(@"    <Import Include=""System.Collections"" />");
                    streamWriter.WriteLine(@"    <Import Include=""System.Collections.Generic"" />");
                    streamWriter.WriteLine(@"    <Import Include=""System.Data"" />");
                    streamWriter.WriteLine(@"    <Import Include=""System.Diagnostics"" />");
                    streamWriter.WriteLine(@"    <Import Include=""System.Linq"" />");
                    streamWriter.WriteLine(@"    <Import Include=""System.Xml.Linq"" />");
                    streamWriter.WriteLine(@"    <Import Include=""System.Threading.Tasks"" />");
                    streamWriter.WriteLine(@"  </ItemGroup>");
                    streamWriter.WriteLine(@"  <ItemGroup>");
                    streamWriter.WriteLine(@"    <Compile Include=""Program.vb"" />");
                    streamWriter.WriteLine(@"    <Compile Include=""My Project\AssemblyInfo.vb"" />");
                    foreach (var item in project.ConvertedItems) {
                        streamWriter.WriteLine(@"    <Compile Include=""{0}"" />", item.DestinationPath);
                    }
                    streamWriter.WriteLine(@"  </ItemGroup>");
                    streamWriter.WriteLine(@"  <Import Project=""$(MSBuildToolsPath)\Microsoft.VisualBasic.targets"" />");
                    streamWriter.WriteLine(@"  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. ");
                    streamWriter.WriteLine(@"       Other similar extension points exist, see Microsoft.Common.targets.");
                    streamWriter.WriteLine(@"  <Target Name=""BeforeBuild"">");
                    streamWriter.WriteLine(@"  </Target>");
                    streamWriter.WriteLine(@"  <Target Name=""AfterBuild"">");
                    streamWriter.WriteLine(@"  </Target>");
                    streamWriter.WriteLine(@"  -->");
                    streamWriter.WriteLine(@"</Project>");

                }
            }
        }
    }
}
