using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

        public bool GenerateModuleImports(string outputPath, string propertyNamePrefix, string propertyValuePrefix, string[] importFiles, string[] importRelativePaths, string[] packageConfigPaths, string[] moduleInputs)
        {
            var importPaths = importFiles.Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => Path.Combine(outputPath, i.Trim())).ToList();

            if (!importPaths.Any(i => CoreUtility.IsFileOutOfDate(i, moduleInputs)))
            {
                return true;
            }

            ProjectRootElement project = CreateProjectWithNuGetProperties(propertyNamePrefix, propertyValuePrefix, false, packageConfigPaths);

            foreach (string importFile in importPaths)
            {
                var importProject = ProjectRootElement.Create(importFile);

                AddImports(importProject, importRelativePaths, project.Properties.Where(i => i.Name.StartsWith(propertyNamePrefix)).Select(i => String.Format("$({0})", i.Name)));

                importProject.Save();
            }
            return true;
        }

        internal bool Generate(string outputPath, string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, string[] importRelativePaths, string[] packageConfigPaths, string[] beforeModuleImports, string[] afterModuleImports)
        {
            ProjectRootElement project = CreateProjectWithNuGetProperties(propertyNamePrefix, propertyValuePrefix, includeAllVersions, packageConfigPaths);

            if (beforeModuleImports != null)
            {
                foreach (var import in beforeModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
                {
                    import.Condition = String.Format(" Exists('{0}') ", import.Project);
                }
            }

            AddImports(project, importRelativePaths, project.Properties.Where(i => i.Name.StartsWith(propertyNamePrefix)).Select(i => String.Format("$({0})", i.Name)));

            if (afterModuleImports != null)
            {
                foreach (var import in afterModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
                {
                    import.Condition = String.Format(" Exists('{0}') ", import.Project);
                }
            }

            project.Save(outputPath);

            return true;
        }

        private static ProjectRootElement CreateProjectWithNuGetProperties(string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, params string[] packageConfigPaths)
        {
            ProjectRootElement project = ProjectRootElement.Create();

            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();

            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            foreach (var packageConfigPath in packageConfigPaths.Where(i => !String.IsNullOrWhiteSpace(i) && File.Exists(i)))
            {
                XDocument document = XDocument.Load(packageConfigPath);

                if (document.Root != null)
                {
                    foreach (var item in document.Root.Elements(NuGetPackagesConfigPackageElementName).Select(i => new
                    {
                        IdAttribute = i.Attribute(NuGetPackagesConfigIdAttributeName),
                        VersionAttribute = i.Attribute(NuGetPackagesConfigVersionAttributeName),
                    }))
                    {
                        // Skip packages that are missing an 'id' or 'version' attribute or if they specified value is an empty string
                        //
                        if (item.IdAttribute == null || item.VersionAttribute == null || String.IsNullOrWhiteSpace(item.IdAttribute.Value) || String.IsNullOrWhiteSpace(item.VersionAttribute.Value))
                        {
                            continue;
                        }

                        string id = item.IdAttribute.Value;
                        string versionString = item.VersionAttribute.Value;

                        // Generate the property name and value once
                        //
                        string propertyName = String.Format(CultureInfo.CurrentCulture, "{0}{1}", propertyNamePrefix, id.Replace(".", "_"));
                        string propertyValue = String.Format(CultureInfo.CurrentCulture, "{0}{1}.{2}", propertyValuePrefix, id, versionString);

                        propertyGroup.SetProperty(propertyName, propertyValue);

                        Version version;

                        // Parse the version first to see if the package is a candidate for a property.  Valid versions include values like 'Highest' but these
                        //  can not be used for properties because only NuGet ends up knowing the version.  This code would have to query NuGet which would slow down parsing.
                        //
                        if (includeAllVersions && Version.TryParse(versionString, out version))
                        {
                            // Include additional properties for each part of the version.  So if the package ID is 'CompanyA.Product' and the version is '1.20.300', you end up with these properties:
                            // CompanyA_Product_1
                            // CompanyA_Product_1_20
                            // CompanyA_Product_1_20_300
                            //
                            propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}_{1}", propertyName, version.Major), propertyValue);

                            propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}", propertyName, version.Major, version.Minor), propertyValue);

                            if (version.Build > -1)
                            {
                                propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}_{3}", propertyName, version.Major, version.Minor, version.Build), propertyValue);

                                if (version.Revision > -1)
                                {
                                    propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}_{3}_{4}", propertyName, version.Major, version.Minor, version.Build, version.Revision), propertyValue);
                                }
                            }
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
    }
}