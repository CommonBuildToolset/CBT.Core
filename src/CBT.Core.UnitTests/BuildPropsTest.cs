using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Shouldly;
using Xunit;

namespace CBT.Core.UnitTests
{
    /// <summary>
    /// Tests to verify the core build.props file.
    /// </summary>
    public class BuildPropsTest : TestBase
    {
        private readonly ProjectRootElement _project;

        public BuildPropsTest()
        {
            _project = ProjectRootElement.Open(Path.Combine(TestAssemblyDirectory, "build.props"));
        }

        [Fact]
        [Description("Verifies that required properties exist in the build.props file.")]
        public void RequiredPropertiesTest()
        {
            string defaultCondition = " '$({0})' == '' ";
            List<Property> knownProperties = new List<Property>
            {
                new Property("MSBuildAllProjects", @"$(MSBuildAllProjects);$(MSBuildThisFileFullPath)", string.Empty),
                new Property("EnlistmentRoot", @"$({0}.TrimEnd('\\'))", " '$({0})' != '' "),
                new Property("CBTGlobalPath", @"$(MSBuildThisFileDirectory)", defaultCondition),
                new Property("CBTGlobalPath", @"$(CBTGlobalPath.TrimEnd('\\'))", string.Empty),
                new Property("CBTLocalPath", @"$([System.IO.Path]::GetDirectoryName($(CBTGlobalPath)))\Local", @" '$({0})' == '' And Exists('$([System.IO.Path]::GetDirectoryName($(CBTGlobalPath)))\Local') "),
                new Property("CBTLocalPath", @"$(CBTLocalPath.TrimEnd('\\'))", string.Empty),
                new Property("CBTLocalBuildExtensionsPath", @"$(CBTLocalPath)\Extensions", @" '$({0})' == '' And '$(CBTLocalPath)' != '' And Exists('$(CBTLocalPath)\Extensions') "),
                new Property("Configuration", @"$(DefaultProjectConfiguration)", @" '$({0})' == '' And '$(DefaultProjectConfiguration)' != '' "),
                new Property("Platform", @"$(DefaultProjectPlatform)", @" '$({0})' == '' And '$(DefaultProjectPlatform)' != '' "),
                new Property("CBTModulePackageConfigPath", @"$([System.IO.Path]::Combine($(CBTLocalPath), 'CBTModules', 'CBTModules.proj'))", @" '$({0})' == '' And '$(CBTLocalPath)' != '' And Exists('$(CBTLocalPath)\CBTModules\CBTModules.proj') "),
                new Property("CBTModulePackageConfigPath", @"$([System.IO.Path]::Combine($(CBTLocalPath), 'CBTModules.proj'))", @" '$({0})' == '' And '$(CBTLocalPath)' != '' And Exists('$(CBTLocalPath)\CBTModules.proj') "),
                new Property("CBTModulePackageConfigPath", @"$([System.IO.Path]::Combine($(CBTLocalPath), 'CBTModules', 'packages.config'))", @" '$({0})' == '' And '$(CBTLocalPath)' != '' And Exists('$(CBTLocalPath)\CBTModules\packages.config') "),
                new Property("CBTModulePackageConfigPath", @"$([System.IO.Path]::Combine($(CBTLocalPath), 'packages.config'))", @" '$({0})' == '' And '$(CBTLocalPath)' != '' And Exists('$(CBTLocalPath)\packages.config') "),
                new Property("CBTModulePackageConfigPath", @"$([System.IO.Path]::GetFullPath($({0})))", @" '$({0})' != '' "),
                new Property("CBTPackagesFallbackPath", @"$([System.IO.Path]::Combine($(SolutionDir), 'packages'))", @" '$({0})' == '' And '$(SolutionDir)' != '' And '$(SolutionDir)' != '*Undefined*' And Exists('$(SolutionDir)')"),
                new Property("CBTPackagesFallbackPath", @"$([System.IO.Path]::Combine($([System.IO.Path]::GetDirectoryName($(MSBuildProjectDirectory))), 'packages'))", defaultCondition),
                new Property("CBTCoreAssemblyPath", @"$(MSBuildThisFileDirectory)CBT.Core.dll", defaultCondition),
                new Property("CBTModuleRestoreInputs", @"$(MSBuildThisFileFullPath);$(CBTCoreAssemblyPath);$(CBTModulePackageConfigPath)", defaultCondition),
                new Property("CBTIntermediateOutputPath", @"$(MSBuildThisFileDirectory)obj", defaultCondition),
                new Property("CBTModulePath", @"$(CBTIntermediateOutputPath)\Modules", defaultCondition),
                new Property("CBTModulePropertiesFile", @"$(CBTModulePath)\$(MSBuildThisFile)", defaultCondition),
                new Property("CBTModuleExtensionsPath", @"$(CBTModulePath)\Extensions", defaultCondition),
                new Property("CBTModuleImportsBefore", @"%24(CBTLocalBuildExtensionsPath)\%24(MSBuildThisFile)", defaultCondition),
                new Property("CBTModuleImportsAfter", string.Empty, defaultCondition),
                new Property("CBTNuGetBinDir", @"$(CBTIntermediateOutputPath)\NuGet", defaultCondition),
                new Property("CBTNuGetDownloaderAssemblyPath", @"$(CBTCoreAssemblyPath)", defaultCondition),
                new Property("CBTNuGetDownloaderClassName", @"CBT.Core.Internal.DefaultNuGetDownloader", defaultCondition),
                new Property("CBTModuleRestoreTaskName", @"CBT.Core.Tasks.RestoreModules", defaultCondition),
                new Property("CBTModuleRestoreCommand", @"$(CBTNuGetBinDir)\NuGet.exe", defaultCondition),
                new Property("CBTModuleRestoreCommandArguments", @"restore ""$(CBTModulePackageConfigPath)"" -NonInteractive", defaultCondition),
                new Property("CBTModuleRestoreCommandArguments", @"$(CBTModuleRestoreCommandArguments) $(CBTModuleRestoreCommandAdditionalArguments)", @" '$(CBTModuleRestoreCommandAdditionalArguments)' != '' "),
                new Property("CBTModuleNuGetAssetsFlagFile", @"$(CBTIntermediateOutputPath)\AssetsLockFilePath.flag", defaultCondition),
                new Property("RestoreCBTModules", @"false", @" $(RestoreGraphProjectInput.Contains($(CBTModulePackageConfigPath))) "),
                new Property("CBTCoreAssemblyName", @"$(CBTCoreAssemblyPath.GetType().Assembly.GetType('System.AppDomain').GetProperty('CurrentDomain').GetValue(null).SetData('CBT_CORE_ASSEMBLY', $(CBTCoreAssemblyPath.GetType().Assembly.GetType('System.AppDomain').GetProperty('CurrentDomain').GetValue(null).Load($(CBTCoreAssemblyPath.GetType().Assembly.GetType('System.IO.File').GetMethod('ReadAllBytes').Invoke(null, $([System.IO.Directory]::GetFiles($([System.IO.Path]::GetDirectoryName($(CBTCoreAssemblyPath))), $([System.IO.Path]::GetFileName($(CBTCoreAssemblyPath)))))))))))", @" Exists('$(CBTCoreAssemblyPath)') And '$(CBTCoreAssemblyPath.GetType().Assembly.GetType(`System.AppDomain`).GetProperty(`CurrentDomain`).GetValue(null).GetData(`CBT_CORE_ASSEMBLY`))' == '' "),
                new Property("CBTCoreAssemblyName", @"$(CBTCoreAssemblyPath.GetType().Assembly.GetType('System.AppDomain').GetProperty('CurrentDomain').GetValue(null).GetData('CBT_CORE_ASSEMBLY'))", string.Empty),
                new Property("CBTModulesRestored", @"$(CBTCoreAssemblyPath.GetType().Assembly.GetType('System.AppDomain').GetProperty('CurrentDomain').GetValue(null).GetData('CBT_CORE_ASSEMBLY').CreateInstance($(CBTModuleRestoreTaskName)).Execute($(CBTModuleImportsAfter.Split(';')), $(CBTModuleImportsBefore.Split(';')), $(CBTModuleExtensionsPath), $(CBTModulePropertiesFile), $(CBTNuGetDownloaderAssemblyPath), $(CBTNuGetDownloaderClassName), '$(CBTNuGetDownloaderArguments)', $(CBTModuleRestoreInputs.Split(';')), $(CBTModulePackageConfigPath), $(NuGetPackagesPath), $(CBTPackagesFallbackPath), $(CBTModuleRestoreCommand), $(CBTModuleRestoreCommandArguments), $(MSBuildProjectFullPath), $(CBTModuleNuGetAssetsFlagFile)))", @" '$(RestoreCBTModules)' != 'false' And '$(BuildingInsideVisualStudio)' != 'true' And '$(CBTModulesRestored)' != 'true' And '$(CBTCoreAssemblyName)' != '' ")
            };
            var propertiesToScan = _project.Properties.Where(p => p.Parent.Parent is ProjectRootElement);
            var propertiesEnumerator = propertiesToScan.GetEnumerator();
            foreach (var knownProperty in knownProperties)
            {
                propertiesEnumerator.MoveNext();
                var property = propertiesEnumerator.Current;
                property.ShouldNotBe(null);

                property.Name.ShouldBe(knownProperty.Name,StringCompareShould.IgnoreCase);

                property.Condition.ShouldBe(String.Format(knownProperty.Condition, property.Name), $"Property {property.Name} condition is not as expected.");
                property.Value.ShouldBe(String.Format(knownProperty.Value, property.Name), $"Property {property.Name} value is not as expected.");
            }
            propertiesEnumerator.Dispose();
            knownProperties.Count.ShouldBe(propertiesToScan.Count(), "Expecting properites under ProjectRootElement and actual properties differ. ");

        }

        [Fact]
        [Description("Verifies that required Items exist in the build.props file.")]
        public void RequiredItemsTest()
        {
            ICollection<Items> items = new List<Items>();
            items.Add(new Items {
                ItemType = "CBTParseError",
                Include = "The 'EnlistmentRoot' property must be set.  Please ensure it is declared in a properties file before CBT Core is imported.",
                Condition = " '$(EnlistmentRoot)' == '' ",
                Metadata = new List<NameValueCondition>
                {
                  new NameValueCondition { Name="Code", Value="CBT1000", Condition = string.Empty }
                }
            });
            items.Add(new Items
            {
                ItemType = "CBTParseError",
                Include = "Modules were not restored and the build cannot continue.  Refer to other errors for more information.",
                Condition = " '$(CBTModulesRestored)' == 'false' ",
                Metadata = new List<NameValueCondition>
                {
                  new NameValueCondition { Name="Code", Value="CBT1001", Condition = string.Empty }
                }
            });
            items.Add(new Items
            {
                ItemType = "CBTParseError",
                Include = "The CBT module configuration file packages.config or CBTModules.proj was not found under $(CBTLocalPath) or $(CBTLocalPath)\\CBTModules.  Please add a CBT module package configuration file or set the property 'CBTModulePackageConfigPath' to your custom location.",
                Condition = " '$(CBTModulePackageConfigPath)' == '' ",
                Metadata = new List<NameValueCondition>
                {
                  new NameValueCondition { Name="Code", Value="CBT1002", Condition = string.Empty }
                }
            });
            ItemShouldBe(items);
        }

        [Fact]
        [Description("Verifies intialTargets are properly defined.")]
        public void InitialTargetsTest()
        {
            _project.InitialTargets.ShouldBe("ShowCBTParseErrors;RestoreCBTModules");
        }

        [Fact]
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

        [Fact]
        [Description("Verifies that the RestoreCBTModules target and RestoreModules task are properly defined.")]
        public void RestoreCBTModulesTargetTest()
        {

            var target = _project.Targets.FirstOrDefault(i => i.Name.Equals("RestoreCBTModules", StringComparison.OrdinalIgnoreCase));

            target.ShouldNotBe(null);

            target.Condition.ShouldBe(@" '$(RestoreCBTModules)' != 'false' And '$(CBTModulesRestored)' != 'true' ");

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
                {"RestoreCommandArguments", "$(CBTModuleRestoreCommandArguments)"},
                {"AssetsFlag", "$(CBTModuleNuGetAssetsFlagFile)"}
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

        [Fact]
        [Description("Verifies that the GenerateAssetFlagFile target is properly defined.")]
        public void GenerateModuleAssetFlagFileTargetTest()
        {

            ProjectTargetElement target = _project.Targets.FirstOrDefault(i => i.Name.Equals("GenerateModuleAssetFlagFile", StringComparison.OrdinalIgnoreCase));

            target.ShouldNotBeNull();

            // ReSharper disable once PossibleNullReferenceException
            target.Children.Count.ShouldBe(2);
            target.ShouldNotBe(null);
            target.Condition.ShouldBe(@" '$(RestoreOutputAbsolutePath)' != '' And '$(MSBuildProjectFullPath)' == '$(CBTModulePackageConfigPath)' ");
            target.Inputs.ShouldBe(string.Empty);
            target.Outputs.ShouldBe(string.Empty);
            target.AfterTargets.ShouldBe("_GenerateRestoreProjectSpec");
            target.BeforeTargets.ShouldBe(string.Empty);
            target.DependsOnTargets.ShouldBe(string.Empty);

            var targetChildrenEnumerator = target.Children.GetEnumerator();
            targetChildrenEnumerator.MoveNext();

            var itemGroup = targetChildrenEnumerator.Current as ProjectItemGroupElement;
            itemGroup.ShouldNotBe(null);
            itemGroup.Children.Count.ShouldBe(5);

            var itemEnumerator = itemGroup.Items.GetEnumerator();
            itemEnumerator.MoveNext();
            var item = itemEnumerator.Current;
            item.ItemType.ShouldBe("RestoreAssetsFlagData");
            item.Remove.ShouldBe("@(RestoreAssetsFlagData)");

            itemEnumerator.MoveNext();
            item = itemEnumerator.Current;
            item.ItemType.ShouldBe("RestoreAssetsFlagData");
            item.Include.ShouldBe("ProjectJsonPath");
            item.Metadata.Count.ShouldBe(1);
            item.Metadata.First().Name.ShouldBe("value");
            item.Metadata.First().Value.ShouldBe("$(_CurrentProjectJsonPath)");

            itemEnumerator.MoveNext();
            item = itemEnumerator.Current;
            item.ItemType.ShouldBe("RestoreAssetsFlagData");
            item.Include.ShouldBe("RestoreProjectStyle");
            item.Metadata.Count.ShouldBe(1);
            item.Metadata.First().Name.ShouldBe("value");
            item.Metadata.First().Value.ShouldBe("$(RestoreProjectStyle)");

            itemEnumerator.MoveNext();
            item = itemEnumerator.Current;
            item.ItemType.ShouldBe("RestoreAssetsFlagData");
            item.Include.ShouldBe("RestoreOutputAbsolutePath");
            item.Metadata.Count.ShouldBe(1);
            item.Metadata.First().Name.ShouldBe("value");
            item.Metadata.First().Value.ShouldBe("$(RestoreOutputAbsolutePath)");

            itemEnumerator.MoveNext();
            item = itemEnumerator.Current;
            item.ItemType.ShouldBe("RestoreAssetsFlagData");
            item.Include.ShouldBe("PackageReference");
            item.Metadata.Count.ShouldBe(2);
            item.Metadata.First().Name.ShouldBe("id");
            item.Metadata.First().Value.ShouldBe("%(PackageReference.Identity)");
            item.Metadata.Last().Name.ShouldBe("version");
            item.Metadata.Last().Value.ShouldBe("%(PackageReference.Version)");
            itemEnumerator.Dispose();

            targetChildrenEnumerator.MoveNext();

            var task1 = targetChildrenEnumerator.Current as ProjectTaskElement;
            task1.ShouldNotBe(null);
            task1.Name.ShouldBe("WriteAssetsFlagJsonFile");
            task1.Condition.ShouldBe(string.Empty);
            task1.Parameters.Count.ShouldBe(2);
            task1.Parameters.ShouldBe(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"File", "$(CBTModuleNuGetAssetsFlagFile)"},
                {"Input", "@(RestoreAssetsFlagData)"},
            });

            targetChildrenEnumerator.Dispose();
        }

        [Fact]
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

        private void ItemShouldBe( IEnumerable<Items> items, StringCompareShould stringCompareShould = StringCompareShould.IgnoreCase)
        {
            var valItemEnumerator = items.GetEnumerator();
            valItemEnumerator.MoveNext();
            foreach (var item in _project.Items)
            {
                // Ignore items not under projectroot
                if (!(item.Parent.Parent is ProjectRootElement))
                {
                    continue;
                }
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
            valItemEnumerator.Dispose();
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
            valMetadataEnumerator.Dispose();
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