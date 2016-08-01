using Microsoft.Build.Construction;
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
        internal const string ImportRelativePath = @"CBT\Module\$(MSBuildThisFile)";

        internal const string ModuleConfigPath = @"CBT\Module\module.config";

        internal const string PropertyNamePrefix = "CBTModule_";

        internal const string PropertyValuePrefix = @"$(NuGetPackagesPath)\";

        /// <summary>
        /// The name of the 'ID' attribute in the NuGet packages.config.
        /// </summary>
        private const string NuGetPackagesConfigIdAttributeName = "id";

        /// <summary>
        /// The name of the &lt;package /&gt; element in th NuGet packages.config.
        /// </summary>
        private const string NuGetPackagesConfigPackageElementName = "package";

        /// <summary>
        /// The name of the 'Version' attribute in the NuGet packages.config.
        /// </summary>
        private const string NuGetPackagesConfigVersionAttributeName = "version";

        private readonly string[] _packageConfigPaths;
        private readonly IDictionary<string, PackageInfo> _packages;
        private readonly string _packagesPath;

        public ModulePropertyGenerator(string packagesPath, params string[] packageConfigPaths)
        {
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
            _packageConfigPaths = packageConfigPaths;

            _packages = ParsePackages();
        }

        public bool Generate(string outputPath, string extensionsPath, string[] beforeModuleImports, string[] afterModuleImports)
        {
            ProjectRootElement project = CreateProjectWithNuGetProperties();

            List<string> properties = project.Properties.Where(i => i.Name.StartsWith(PropertyNamePrefix)).Select(i => $"$({i.Name})").ToList();

            if (beforeModuleImports != null)
            {
                foreach (ProjectImportElement import in beforeModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
                {
                    import.Condition = $" Exists('{import.Project}') ";
                }
            }

            AddImports(project, properties);

            if (afterModuleImports != null)
            {
                foreach (ProjectImportElement import in afterModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
                {
                    import.Condition = $" Exists('{import.Project}') ";
                }
            }

            project.Save(outputPath);

            Parallel.ForEach(GetModuleExtensions(), i =>
            {
                ProjectRootElement extensionProject = ProjectRootElement.Create(Path.Combine(extensionsPath, i.Key.Trim()));

                AddImports(extensionProject, properties);

                extensionProject.Save();
            });

            return true;
        }

        private void AddImports(ProjectRootElement project, IEnumerable<string> modulePaths)
        {
            foreach (ProjectImportElement import in modulePaths.Where(i => !String.IsNullOrWhiteSpace(i)).Select(modulePath => project.AddImport(Path.Combine(modulePath, ImportRelativePath))))
            {
                import.Condition = $"Exists('{import.Project}')";
            }
        }

        private ProjectRootElement CreateProjectWithNuGetProperties()
        {
            ProjectRootElement project = ProjectRootElement.Create();

            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();

            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            propertyGroup.SetProperty("CBTAllModulePaths", String.Join(";", _packages.Values.Select(i => $"{i.Id}={i.Path}")));

            foreach (PackageInfo item in _packages.Values)
            {
                // Generate the property name and value once
                //
                string propertyName = $"{PropertyNamePrefix}{item.Id.Replace(".", "_")}";
                string propertyValue = $"{PropertyValuePrefix}{item.Id}.{item.VersionString}";

                propertyGroup.SetProperty(propertyName, propertyValue);
            }

            return project;
        }

        private IDictionary<string, string> GetModuleExtensions()
        {
            ConcurrentDictionary<string, string> extensionImports = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Parallel.ForEach(_packages.Values, packageInfo =>
            {
                string path = Path.Combine(packageInfo.Path, ModuleConfigPath);

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

            return extensionImports;
        }

        private IDictionary<string, PackageInfo> ParsePackages()
        {
            IDictionary<string, PackageInfo> packages = new Dictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (string packageConfigPath in _packageConfigPaths.Where(i => !String.IsNullOrWhiteSpace(i) && File.Exists(i)))
            {
                XDocument document = XDocument.Load(packageConfigPath);

                if (document.Root != null)
                {
                    foreach (var item in document.Root.Elements(NuGetPackagesConfigPackageElementName).Select(i => new
                    {
                        Id = i.Attribute(NuGetPackagesConfigIdAttributeName) == null ? null : i.Attribute(NuGetPackagesConfigIdAttributeName).Value,
                        Version = i.Attribute(NuGetPackagesConfigVersionAttributeName) == null ? null : i.Attribute(NuGetPackagesConfigVersionAttributeName).Value,
                    }))
                    {
                        // Skip packages that are missing an 'id' or 'version' attribute or if they specified value is an empty string
                        //
                        if (item.Id == null || item.Version == null ||
                            String.IsNullOrWhiteSpace(item.Id) ||
                            String.IsNullOrWhiteSpace(item.Version))
                        {
                            continue;
                        }

                        PackageInfo packageInfo = new PackageInfo(item.Id, item.Version, Path.Combine(_packagesPath, $"{item.Id}.{item.Version}"));

                        if (packages.ContainsKey(packageInfo.Id))
                        {
                            packages[packageInfo.Id] = packageInfo;
                        }
                        else
                        {
                            packages.Add(packageInfo.Id, packageInfo);
                        }
                    }
                }
            }

            return packages;
        }
    }
}