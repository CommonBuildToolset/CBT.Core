using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CBT.Core.Internal
{
    internal sealed class ModulePropertyGenerator
    {
        internal static readonly string ImportRelativePath = "build";
        internal static readonly string ModuleConfigPath = Path.Combine(ImportRelativePath, "module.config");
        internal static readonly string ModuleRelativePathV1 = Path.Combine("CBT", "Module");
        internal static readonly string PropertyNamePrefix = "CBTModule_";
        internal static readonly string PropertyValuePrefix = $"$(NuGetPackagesPath){Path.DirectorySeparatorChar}";
        private readonly CBTTaskLogHelper _log;
        private readonly IDictionary<string, PackageIdentityWithPath> _packages;
        private readonly string _packagesPath;

        public ModulePropertyGenerator(CBTTaskLogHelper logHelper, string packagesPath, string assetsFileDirectory, params string[] packageConfigPaths)
            : this(new List<INuGetPackageConfigParser>
            {
                new NuGetPackagesConfigParser(),
                new NuGetProjectJsonParser(),
                new NuGetPackageReferenceProjectParser(logHelper)
            }, packagesPath, assetsFileDirectory, packageConfigPaths)
        {
            _log = logHelper;
        }

        public ModulePropertyGenerator(IList<INuGetPackageConfigParser> configParsers, string packagesPath, string assetsFileDirectory, params string[] packageConfigPaths)
        {
            if (configParsers == null)
            {
                throw new ArgumentNullException(nameof(configParsers));
            }

            if (String.IsNullOrWhiteSpace(packagesPath))
            {
                throw new ArgumentNullException(nameof(packagesPath));
            }

            if (!Directory.Exists(packagesPath))
            {
                throw new DirectoryNotFoundException($"Could not find part of the path '{packagesPath}'");
            }

            if (packageConfigPaths == null)
            {
                throw new ArgumentNullException(nameof(packageConfigPaths));
            }

            _packagesPath = packagesPath;
            _packages = packageConfigPaths
                .SelectMany(i => configParsers
                .SelectMany(parser => parser.GetPackages(packagesPath, i, assetsFileDirectory)))
                .ToDictionary(i => $"{i.Id}.{i.Version}", i => i, StringComparer.OrdinalIgnoreCase);
        }

        public bool Generate(string outputPath, string extensionsPath, string[] beforeModuleImports, string[] afterModuleImports)
        {
            _log.LogMessage(MessageImportance.Low, $"Modules:");
            foreach (PackageIdentityWithPath package in _packages.Values)
            {
                _log.LogMessage(MessageImportance.Low, $"  {package.Id} {package.Version}");
            }
            ProjectRootElement project = CreateProjectWithNuGetProperties(outputPath);


            if (beforeModuleImports != null)
            {
                foreach (ProjectImportElement import in beforeModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
                {
                    import.Condition = $" Exists('{import.Project}') ";
                }
            }

            AddImports(project, _packages.Values);

            if (afterModuleImports != null)
            {
                foreach (ProjectImportElement import in afterModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
                {
                    import.Condition = $" Exists('{import.Project}') ";
                }
            }


            _log.LogMessage(MessageImportance.Low, $"Saving import file '{outputPath}'.");

            project.Save();

            foreach (string item in GetModuleExtensions().Select(i => i.Key.Trim()))
            {
                ProjectRootElement extensionProject = ProjectRootElement.Create(Path.Combine(extensionsPath, item));

                AddImports(extensionProject, _packages.Values);

                _log.LogMessage(MessageImportance.Low, $"Saving import file '{extensionProject.FullPath}'.");

                extensionProject.Save();
            }

            return true;
        }

        private void AddImports(ProjectRootElement project, IEnumerable<PackageIdentityWithPath> modulePackages)
        {
            foreach (var modulePackage in modulePackages.Where(i => !string.IsNullOrWhiteSpace(i?.Path)))
            {
                // For cbt module build packages import the packageId.Props into the build.props file.
                // For non cbt module build packages do nothing let cbt.nuget handle them.
                // For cbt extension imports use the extension filename.
                // V1 modules cbt\module will keep existing import logic of conditional existance and referencing $(MSBuildThisFile).
                // Once V1 module support is removed this logic can be simplified.
                string importFileName =
                    Path.GetFileName(project?.FullPath ?? string.Empty)
                        .Equals("build.props", StringComparison.OrdinalIgnoreCase)
                        ? $"{modulePackage.Id}.props"
                        : Path.GetFileName(project?.FullPath ?? "$(MSBuildThisFile)");
                bool v1Package = Directory.Exists(Path.Combine(modulePackage.FullPath, ModuleRelativePathV1));
                bool isCbtModulePackage = v1Package || File.Exists(Path.Combine(modulePackage.FullPath, ModuleConfigPath));
                string importPath = v1Package ? Path.Combine(ModuleRelativePathV1, "$(MSBuildThisFile)") : Path.Combine(ImportRelativePath, importFileName);
                if (v1Package || (File.Exists(Path.Combine(modulePackage.FullPath, importPath)) && isCbtModulePackage))
                {
                    ProjectImportElement importElement = project?.AddImport(Path.Combine(PropertyValuePrefix, modulePackage.Path, importPath));
                    if (v1Package && importElement != null)
                    {
                        importElement.Condition = $" Exists('{importElement.Project}') ";
                    }
                }
            }
        }

        private ProjectRootElement CreateProjectWithNuGetProperties(string projectName)
        {
            ProjectRootElement project = ProjectRootElement.Create(projectName);

            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();

            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");
            ProjectPropertyElement nuGetPackagesPathProperty = propertyGroup.AddProperty("NuGetPackagesPath", _packagesPath);

            nuGetPackagesPathProperty.Condition = " '$(NuGetPackagesPath)' == '' ";

            propertyGroup.SetProperty("CBTAllModulePaths", String.Join(";", _packages.Values.Select(i => $"{i.Id}={PropertyValuePrefix}{i.Path}")));

            foreach (PackageIdentityWithPath item in _packages.Values)
            {
                // Generate the property name and value once
                //
                string propertyName = $"{PropertyNamePrefix}{item.Id.Replace(".", "_")}";
                string propertyValue = $"{PropertyValuePrefix}{item.Path}";

                propertyGroup.SetProperty(propertyName, propertyValue);
            }

            return project;
        }

        private IDictionary<string, string> GetModuleExtensions()
        {
            ConcurrentDictionary<string, string> extensionImports = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Parallel.ForEach(_packages.Values, packageInfo =>
            {
                string path = Path.Combine(packageInfo.FullPath, ModuleConfigPath);

                if (Directory.Exists(Path.Combine(packageInfo.FullPath, ModuleRelativePathV1)))
                {
                    path = Path.Combine(packageInfo.FullPath, ModuleRelativePathV1, "module.config");
                }

                if (File.Exists(path))
                {
                    XDocument document = XDocument.Load(path);

                    XElement extensionImportsElement = document.Root?.Element("extensionImports");

                    if (extensionImportsElement != null)
                    {
                        foreach (string item in extensionImportsElement.Elements("add").Select(i => i.Attribute("name")).Where(i => !String.IsNullOrWhiteSpace(i?.Value)).Select(i => i.Value))
                        {
                            extensionImports.TryAdd(item, packageInfo.Id);
                        }
                    }
                }
            });

            _log.LogMessage(MessageImportance.Low, $"Module extensions:");

            foreach (KeyValuePair<string, string> item in extensionImports)
            {
                _log.LogMessage(MessageImportance.Low, $"  {item.Key} ({item.Value})");
            }

            return extensionImports;
        }
    }
}