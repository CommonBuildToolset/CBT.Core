using Microsoft.Build.Construction;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CBT.Core.Internal
{
    internal sealed class ModulePropertyGenerator
    {
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
        private readonly Lazy<IDictionary<string, PackageInfo>> _packagesLazy;
        private readonly string _packagesPath;

        public ModulePropertyGenerator(string packagesPath, params string[] packageConfigPaths)
        {
            if (String.IsNullOrWhiteSpace(packagesPath))
            {
                throw new ArgumentNullException("packagesPath");
            }

            if (!Directory.Exists(packagesPath))
            {
                throw new DirectoryNotFoundException(String.Format(CultureInfo.CurrentCulture, "Could not find part of the path '{0}'", packagesPath));
            }

            if (packageConfigPaths == null)
            {
                throw new ArgumentNullException("packageConfigPaths");
            }

            _packagesPath = packagesPath;
            _packageConfigPaths = packageConfigPaths;

            _packagesLazy = new Lazy<IDictionary<string, PackageInfo>>(ParsePackages, isThreadSafe: true);
        }

        public IDictionary<string, PackageInfo> Packages
        {
            get { return _packagesLazy.Value; }
        }

        public bool Generate(string outputPath, string extensionsPath, string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, string[] importRelativePaths, string[] beforeModuleImports, string[] afterModuleImports)
        {
            ProjectRootElement project = CreateProjectWithNuGetProperties(propertyNamePrefix, propertyValuePrefix, includeAllVersions, Packages);

            var properties = project.Properties.Where(i => i.Name.StartsWith(propertyNamePrefix)).Select(i => String.Format("$({0})", i.Name)).ToList();

            if (beforeModuleImports != null)
            {
                foreach (var import in beforeModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
                {
                    import.Condition = String.Format(" Exists('{0}') ", import.Project);
                }
            }

            AddImports(project, importRelativePaths, properties);

            if (afterModuleImports != null)
            {
                foreach (var import in afterModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
                {
                    import.Condition = String.Format(" Exists('{0}') ", import.Project);
                }
            }

            project.Save(outputPath);

            Parallel.ForEach(GetModuleExtensions(), i =>
            {
                var extensionProject = ProjectRootElement.Create(Path.Combine(extensionsPath, i.Trim()));

                AddImports(extensionProject, importRelativePaths, properties);

                extensionProject.Save();
            });

            return true;
        }

        private static ProjectRootElement CreateProjectWithNuGetProperties(string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, IDictionary<string, PackageInfo> packages)
        {
            ProjectRootElement project = ProjectRootElement.Create();

            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();

            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            foreach (var item in packages.Values)
            {
                // Generate the property name and value once
                //
                string propertyName = String.Format(CultureInfo.CurrentCulture, "{0}{1}", propertyNamePrefix, item.Id.Replace(".", "_"));
                string propertyValue = String.Format(CultureInfo.CurrentCulture, "{0}{1}.{2}", propertyValuePrefix, item.Id, item.VersionString);

                propertyGroup.SetProperty(propertyName, propertyValue);

                // Parse the version first to see if the package is a candidate for a property.  Valid versions include values like 'Highest' but these
                //  can not be used for properties because only NuGet ends up knowing the version.  This code would have to query NuGet which would slow down parsing.
                //
                if (includeAllVersions && item.Version != null)
                {
                    // Include additional properties for each part of the version.  So if the package ID is 'CompanyA.Product' and the version is '1.20.300', you end up with these properties:
                    // CompanyA_Product_1
                    // CompanyA_Product_1_20
                    // CompanyA_Product_1_20_300
                    //
                    propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}_{1}", propertyName, item.Version.Major), propertyValue);

                    propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}", propertyName, item.Version.Major, item.Version.Minor), propertyValue);

                    if (item.Version.Build > -1)
                    {
                        propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}_{3}", propertyName, item.Version.Major, item.Version.Minor, item.Version.Build), propertyValue);

                        if (item.Version.Revision > -1)
                        {
                            propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}_{3}_{4}", propertyName, item.Version.Major, item.Version.Minor, item.Version.Build, item.Version.Revision), propertyValue);
                        }
                    }
                }
            }

            return project;
        }

        private void AddImports(ProjectRootElement project, IEnumerable<string> importRelativePaths, IEnumerable<string> modulePaths)
        {
            foreach (var import in modulePaths.Where(i => !String.IsNullOrWhiteSpace(i)).SelectMany(modulePath => importRelativePaths.Where(i => !String.IsNullOrWhiteSpace(i)).Select(importRelativePath => project.AddImport(Path.Combine(modulePath, importRelativePath)))))
            {
                import.Condition = String.Format("Exists('{0}')", import.Project);
            }
        }

        private ICollection<string> GetModuleExtensions()
        {
            ConcurrentDictionary<string, string> extensionImports = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Parallel.ForEach(Packages.Values, packageInfo =>
            {
                var moduleConfigPath = Path.Combine(packageInfo.Path, "module.config");

                if (File.Exists(moduleConfigPath))
                {
                    XDocument document = XDocument.Load(moduleConfigPath);

                    if (document.Root != null)
                    {
                        var extensionImportsElement = document.Root.Element("extensionImports");

                        if (extensionImportsElement != null)
                        {
                            foreach (var item in extensionImportsElement.Elements("add").Select(i => i.Attribute("name")).Where(i => i != null && !String.IsNullOrWhiteSpace(i.Value)).Select(i => i.Value))
                            {
                                extensionImports.TryAdd(item, item);
                            }
                        }
                    }
                }
            });

            return extensionImports.Keys;
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

                        PackageInfo packageInfo = new PackageInfo(item.Id, item.Version, Path.Combine(_packagesPath, String.Format("{0}.{1}", item.Id, item.Version)));

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