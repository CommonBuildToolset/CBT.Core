using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CBT.Core.Internal;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;

namespace CBT.Core.Tasks
{
    public sealed class GenerateNuGetProperties : TaskBase
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

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string PropertyNamePrefix { get; set; }

        public string PropertyValuePrefix { get; set; }

        public bool IncludeAllVersions { get; set; }

        public string[] PackageConfigPaths { get; set; }

        public override bool Execute()
        {
            // Return if no package.config files were specified
            //
            if (PackageConfigPaths.Length == 0)
            {
                return false;
            }

            // Determine if the output path is already up-to-date and do a no-op
            //
            if (File.Exists(OutputPath) && CoreUtility.IsFileOutOfDate(OutputPath, PackageConfigPaths))
            {
                return true;
            }

            ProjectRootElement project = CreateProjectWithNuGetProperties(PropertyNamePrefix, PropertyValuePrefix, IncludeAllVersions, PackageConfigPaths);

            project.Save(OutputPath);

            return true;
        }

        public static ProjectRootElement CreateProjectWithNuGetProperties(string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, params string[] packageConfigPaths)
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

                        Version version;

                        // Parse the version first to see if the package is a candidate for a property.  Valid versions include values like 'Highest' but these
                        //  can not be used for properties because only NuGet ends up knowing the version.  This code would have to query NuGet which would slow down parsing.
                        //
                        if (Version.TryParse(versionString, out version))
                        {
                            // Generate the property name and value once
                            //
                            string propertyName = String.Format(CultureInfo.CurrentCulture, "{0}{1}", propertyNamePrefix, id.Replace(".", "_"));
                            string propertyValue = String.Format(CultureInfo.CurrentCulture, "{0}{1}.{2}", propertyValuePrefix, id, versionString);

                            propertyGroup.SetProperty(propertyName, propertyValue);

                            if (includeAllVersions)
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
            }

            return project;
        }

        
    }
}
