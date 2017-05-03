using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using NUnit.Framework;
using Shouldly;

namespace CBT.Core.UnitTests
{
    /// <summary>
    /// Tests to verify the core build.props file.
    /// </summary>
    [TestFixture]
    public class BuildPropsTest
    {
        private ProjectRootElement _project;

        [OneTimeSetUp]
        public void TestInitialize()
        {
            _project = ProjectRootElement.Open(Path.Combine(TestContext.CurrentContext.TestDirectory, "build.props"));
        }

        [Test]
        [Description("Verifies that required properties exist in the build.props file.")]
        public void RequiredPropertiesTest()
        {
            IDictionary<string, string> knownProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"CBTCoreAssemblyPath", null},
                {"CBTIntermediateOutputPath", null},
                {"CBTModulePackageConfigPath", @" '$({0})' == '' And '$(CBTLocalPath)' != '' And Exists('$(CBTLocalPath)\CBTModules\project.json') "},
                {"CBTModulePath", null},
                {"CBTModulePropertiesFile", null},
                {"CBTModuleExtensionsPath", null},
                {"CBTModuleImportsBefore", null},
                {"CBTModuleImportsAfter", null},
                {"CBTNuGetBinDir", null },
                {"CBTNuGetDownloaderAssemblyPath",  null},
                {"CBTNuGetDownloaderClassName", null },
                {"CBTModuleRestoreTaskName", null},
                {"CBTModuleRestoreCommand", null},
                {"CBTModuleRestoreCommandArguments", null},
                {"CBTModuleRestoreInputs", null},
                {"CBTModulesRestored", " '$(RestoreCBTModules)' != 'false' And '$(BuildingInsideVisualStudio)' != 'true' And '$({0})' != 'true' And Exists('$(CBTCoreAssemblyPath)') "}
            };

            foreach (var knownProperty in knownProperties)
            {
                var property = _project.Properties.FirstOrDefault(i => i.Name.Equals(knownProperty.Key, StringComparison.OrdinalIgnoreCase));

                property.ShouldNotBe(null);

                property.Condition.ShouldBe(String.Format(knownProperty.Value ?? " '$({0})' == '' ", property.Name));
            }

            PropertyShouldBe("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");
            PropertyShouldBe("EnlistmentRoot", @"$(EnlistmentRoot.TrimEnd('\\'))");
            PropertyShouldBe("CBTNuGetBinDir", @"$(CBTIntermediateOutputPath)\NuGet");
            PropertyShouldBe("CBTNuGetDownloaderAssemblyPath", "$(CBTCoreAssemblyPath)");
            PropertyShouldBe("CBTNuGetDownloaderClassName", "CBT.Core.Internal.DefaultNuGetDownloader");

            var globalPathProperty = _project.Properties.Where(i => i.Name.Equals("CBTGlobalPath")).ToList();
            globalPathProperty.Count.ShouldBe(2);
            globalPathProperty[0].Condition.ShouldBe($" '$({globalPathProperty[0].Name})' == '' ", StringCompareShould.IgnoreCase);
            globalPathProperty[0].Value.ShouldBe("$(MSBuildThisFileDirectory)", StringCompareShould.IgnoreCase);
            globalPathProperty[1].Value.ShouldBe($@"$({globalPathProperty[0].Name}.TrimEnd('\\'))", StringCompareShould.IgnoreCase);

            var localPathProperty = _project.Properties.Where(i => i.Name.Equals("CBTLocalPath")).ToList();
            localPathProperty.Count.ShouldBe(2);
            localPathProperty[0].Condition.ShouldBe($@" '$({localPathProperty[0].Name})' == '' And Exists('$([System.IO.Path]::GetDirectoryName($({globalPathProperty[0].Name})))\Local') ", StringCompareShould.IgnoreCase);
            localPathProperty[0].Value.ShouldBe(@"$([System.IO.Path]::GetDirectoryName($(CBTGlobalPath)))\Local", StringCompareShould.IgnoreCase);
            localPathProperty[1].Value.ShouldBe($@"$({localPathProperty[1].Name}.TrimEnd('\\'))", StringCompareShould.IgnoreCase);

            var localBuildExtensionsPathProperty = _project.Properties.Where(i => i.Name.Equals("CBTLocalBuildExtensionsPath")).ToList();
            localBuildExtensionsPathProperty.Count.ShouldBe(1);

            localBuildExtensionsPathProperty[0].Condition.ShouldBe($@" '$({localBuildExtensionsPathProperty[0].Name})' == '' And '$({localPathProperty[0].Name})' != '' And Exists('$({localPathProperty[0].Name})\Extensions') ");
            localBuildExtensionsPathProperty[0].Value.ShouldBe($@"$({localPathProperty[0].Name})\Extensions");
        }

        [Test]
        [Description("Verifies that required Items exist in the build.props file.")]
        public void RequiredItemsTest()
        {
            ICollection<Items> items = new List<Items>();
            items.Add(new Items {
                ItemType = "CBTParseError",
                Include = "The 'EnlistmentRoot' property must be set.  Please ensure it is declared in a properties file before CBT Core is imported.",
                Condition = " '$(EnlistmentRoot)' == '' ",
                Metadata = new List<NameValueCondition>()
                {
                  new NameValueCondition { Name="Code", Value="CBT1000", Condition = string.Empty }
                }
            });
            items.Add(new Items
            {
                ItemType = "CBTParseError",
                Include = "Modules were not restored and the build cannot continue.  Refer to other errors for more information.",
                Condition = " '$(CBTModulesRestored)' == 'false' ",
                Metadata = new List<NameValueCondition>()
                {
                  new NameValueCondition { Name="Code", Value="CBT1001", Condition = string.Empty }
                }
            });
            items.Add(new Items
            {
                ItemType = "CBTParseError",
                Include = "The 'CBTModulePackageConfigPath' is not set or no package configuration file was found in the expected location.  You must have a packages.config or project.json file that specifies what modules to use.",
                Condition = " '$(CBTModulePackageConfigPath)' == '' ",
                Metadata = new List<NameValueCondition>()
                {
                  new NameValueCondition { Name="Code", Value="CBT1002", Condition = string.Empty }
                }
            });
            ItemShouldBe(items);
        }

        [Test]
        [Description("Verifies intialTargets are properly defined.")]
        public void InitialTargetsTest()
        {
            _project.InitialTargets.ShouldBe("ShowCBTParseErrors;RestoreCBTModules");
        }

        [Test]
        [Description("Verifies that the ShowCBTParseErrors target and Error task are properly defined.")]
        public void ShowCBTParseErrorsTargetTest()
        {

            var target = _project.Targets.FirstOrDefault(i => i.Name.Equals("ShowCBTParseErrors", StringComparison.OrdinalIgnoreCase));
            target.ShouldNotBe(null);
            target.Condition.ShouldBe(" '@(CBTParseError)' != '' ");
            target.Inputs.ShouldBe("");
            target.Outputs.ShouldBe("");

            var task = target.Tasks.FirstOrDefault(i => i.Name.Equals("Error"));

            task.ShouldNotBe(null);

            task.Parameters.ShouldBe(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"Text", "%(CBTParseError.Identity)"},
                {"Code", "%(CBTParseError.Code)"}
            });

        }

        [Test]
        [Description("Verifies that the RestoreCBTModules target and RestoreModules task are properly defined.")]
        public void RestoreCBTModulesTargetTest()
        {

            var target = _project.Targets.FirstOrDefault(i => i.Name.Equals("RestoreCBTModules", StringComparison.OrdinalIgnoreCase));

            target.ShouldNotBe(null);

            target.Condition.ShouldBe(" '$(RestoreCBTModules)' != 'false' And '$(CBTModulesRestored)' != 'true' ");

            target.Inputs.ShouldBe("$(CBTModuleRestoreInputs)");

            target.Outputs.ShouldBe("$([MSBuild]::ValueOrDefault($(CBTModulePropertiesFile), 'null'))");

            var task = target.Tasks.FirstOrDefault(i => i.Name.Equals("RestoreModules"));

            task.ShouldNotBe(null);

            task.Parameters.ShouldBe(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"AfterImports", "$(CBTModuleImportsAfter.Split(';'))"},
                {"BeforeImports", "$(CBTModuleImportsBefore.Split(';'))"},
                {"ExtensionsPath", "$(CBTModuleExtensionsPath)"},
                {"ImportsFile", "$(CBTModulePropertiesFile)"},
                {"NuGetDownloaderAssemblyPath", "$(CBTNuGetDownloaderAssemblyPath)"},
                {"NuGetDownloaderClassName", "$(CBTNuGetDownloaderClassName)"},
                {"NuGetDownloaderArguments", "$(CBTNuGetDownloaderArguments)"},
                {"PackageConfig", "$(CBTModulePackageConfigPath)"},
                {"PackagesFallbackPath", "$(CBTPackagesFallbackPath)"},
                {"PackagesPath", "$(NuGetPackagesPath)"},
                {"ProjectFullPath","$(MSBuildProjectFullPath)"},
                {"RestoreCommand", "$(CBTModuleRestoreCommand)"},
                {"RestoreCommandArguments", "$(CBTModuleRestoreCommandArguments)"}
            });

            var propertyGroup = target.PropertyGroups.LastOrDefault();

            propertyGroup.ShouldNotBe(null);

            propertyGroup.Location.ShouldBeGreaterThan(task.Location, "<PropertyGroup /> should come after <RestoreModules /> task");

            var property = propertyGroup.Properties.FirstOrDefault(i => i.Name.Equals("CBTModulesRestored", StringComparison.OrdinalIgnoreCase));

            property.ShouldNotBe(null);

            property.Condition.ShouldBe($" '$({property.Name})' != 'true' ");

            property.Value.ShouldBe(true.ToString(), StringCompareShould.IgnoreCase);

            var usingTask = _project.UsingTasks.FirstOrDefault(i => i.TaskName.Equals("RestoreModules", StringComparison.OrdinalIgnoreCase));

            usingTask.ShouldNotBe(null);

            usingTask.AssemblyFile.ShouldBe("$(CBTCoreAssemblyPath)", StringCompareShould.IgnoreCase);
        }

        [Test]
        [Description("Verifies that imports are correct.")]
        public void ImportsTest()
        {
            var beforeImport = _project.Imports.FirstOrDefault(i => i.Project.Equals(@"$(CBTLocalBuildExtensionsPath)\Before.$(MSBuildThisFile)", StringComparison.OrdinalIgnoreCase));

            beforeImport.ShouldNotBe(null);

            beforeImport.Condition.ShouldBe(@" '$(CBTLocalBuildExtensionsPath)' != '' And Exists('$(CBTLocalBuildExtensionsPath)\Before.$(MSBuildThisFile)') ");

            var firstPropertyGroup = _project.PropertyGroups.First();
            var secondPropertyGroup = _project.PropertyGroups.Skip(1).First();

            beforeImport.Location.ShouldBeGreaterThan(firstPropertyGroup.Location, @"The import of '$(CBTLocalBuildExtensionsPath)\Before.$(MSBuildThisFile)' should come after the first <PropertyGroup />");

            secondPropertyGroup.Location.ShouldBeGreaterThan(beforeImport.Location, @"The import of '$(CBTLocalBuildExtensionsPath)\Before.$(MSBuildThisFile)' should come before the second <PropertyGroup />");

            var afterImport = _project.Imports.FirstOrDefault(i => i.Project.Equals(@"$(CBTLocalBuildExtensionsPath)\After.$(MSBuildThisFile)", StringComparison.OrdinalIgnoreCase));

            afterImport.ShouldNotBe(null);

            afterImport.Condition.ShouldBe(@" '$(CBTLocalBuildExtensionsPath)' != '' And Exists('$(CBTLocalBuildExtensionsPath)\After.$(MSBuildThisFile)') ");

            var lastChild = _project.Children.Last();

            afterImport.ShouldBe(lastChild, @"The last element should be the import of '$(CBTLocalBuildExtensionsPath)\After.$(MSBuildThisFile'");

            var moduleImport = _project.Imports.FirstOrDefault(i => i.Project.Equals("$(CBTModulePropertiesFile)", StringComparison.OrdinalIgnoreCase));

            moduleImport.ShouldNotBe(null);
            moduleImport.Condition.ShouldBe(" ('$(CBTModulesRestored)' == 'true' Or '$(BuildingInsideVisualStudio)' == 'true') And Exists('$(CBTModulePropertiesFile)') ");

            moduleImport.Location.ShouldBeGreaterThan(secondPropertyGroup.Location, "The import of '$(CBTModulePropertiesFile)' should come after the second property group");
        }

        private void PropertyShouldBe(string name, string value, StringCompareShould stringCompareShould = StringCompareShould.IgnoreCase)
        {
            var property = _project.Properties.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            property.ShouldNotBe(null);

            property.Value.ShouldBe(value, stringCompareShould);
        }

        private void ItemShouldBe( IEnumerable<Items> items, StringCompareShould stringCompareShould = StringCompareShould.IgnoreCase)
        {
            var valItemEnumerator = items.GetEnumerator();
            valItemEnumerator.MoveNext();
            foreach (var item in _project.Items)
            {
                var valItem = valItemEnumerator.Current;
                valItem.ShouldNotBe(null);
                if (item.ItemType.Equals(valItem.ItemType))
                {
                    item.Condition.ShouldBe(valItem.Condition, stringCompareShould);
                    item.Include.ShouldBe(valItem.Include, stringCompareShould);
                    MetadataShouldBe(valItem.Metadata, item);
                    valItemEnumerator.MoveNext();
                }
            }
            valItemEnumerator.Current.ShouldBe(null);
        }

        private void MetadataShouldBe(IEnumerable<NameValueCondition> metadata, ProjectItemElement item, StringCompareShould stringCompareShould = StringCompareShould.IgnoreCase)
        {
            var valMetadataEnumerator = metadata.GetEnumerator();
            valMetadataEnumerator.MoveNext();
            foreach (var meta in item.Metadata)
            {
                var valMeta = valMetadataEnumerator.Current;
                valMeta.ShouldNotBe(null);
                meta.Name.ShouldBe(valMeta.Name);
                meta.Condition.ShouldBe(valMeta.Condition, stringCompareShould);
                meta.Value.ShouldBe(valMeta.Value, stringCompareShould);
                valMetadataEnumerator.MoveNext();
            }
            valMetadataEnumerator.Current.ShouldBe(null);
        }
    }

    class NameValueCondition
    {
        internal string Name { get; set; }
        internal string Value { get; set; }
        internal string Condition { get; set; }
    }

    class Items
    {
        internal string ItemType { get; set; }
        internal string Include { get; set; }
        internal string Condition { get; set; }
        internal IEnumerable<NameValueCondition> Metadata { get; set; }
    }
}