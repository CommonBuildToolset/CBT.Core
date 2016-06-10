using CBT.Core.Internal;
using Microsoft.Build.Construction;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

// ReSharper disable PossibleNullReferenceException

namespace CBT.Core.UnitTests
{
    [TestFixture]
    public class ModulePropertyGeneratorTest
    {
        private readonly string _intermediateOutputPath;

        private readonly IList<PackageInfo> _packages;

        private readonly string _packagesConfigPath;

        private readonly string _packagesPath;

        private readonly IList<Tuple<string, string[]>> _moduleExtensions = new List<Tuple<string, string[]>>
        {
            new Tuple<string, string[]>("Package1", new[] {"before.package1.targets", "after.package1.targets"}),
            new Tuple<string, string[]>("Package2.Thing", new[] {"before.package2.targets"}),
        };

        public ModulePropertyGeneratorTest()
        {
            _intermediateOutputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _packagesConfigPath = Path.Combine(_intermediateOutputPath, "packages.config");
            _packagesPath = Path.Combine(_intermediateOutputPath, "packages");

            _packages = new List<PackageInfo>
            {
                new PackageInfo("Package1", "1.0.0", Path.Combine(_packagesPath, "Package1.1.0.0")),
                new PackageInfo("Package2.Thing", "2.5.1", Path.Combine(_packagesPath, "Package2.Thing.2.5.1")),
                new PackageInfo("Package3.a.b.c.d.e.f", "10.10.9999.9999-beta99", Path.Combine(_packagesPath, "Package3.a.b.c.d.e.f")),
            };
        }

        [Test]
        public void ModulePropertiesAreCreated()
        {
            ModulePropertyGenerator modulePropertyGenerator = new ModulePropertyGenerator(_packagesPath, _packagesConfigPath);

            string outputPath = Path.Combine(_intermediateOutputPath, "build.props");
            string extensionsPath = Path.Combine(_intermediateOutputPath, "Extensions");

            string[] importsBefore = { "before.props", "before2.props" };
            string[] importsAfter = { "after.props", "after2.props" };

            bool success = modulePropertyGenerator.Generate(outputPath, extensionsPath, importsBefore, importsAfter);

            success.ShouldBeTrue();

            File.Exists(outputPath).ShouldBeTrue();

            ProjectRootElement project = ProjectRootElement.Open(outputPath);

            project.ShouldNotBeNull();

            // Verify all properties
            //
            foreach (Tuple<string, string> item in new[]
            {
                new Tuple<string, string>("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)"),
            }.Concat(_packages.Select(i =>
                new Tuple<string, string>(
                    $"{ModulePropertyGenerator.PropertyNamePrefix}{i.Id.Replace(".", "_")}",
                    $"{ModulePropertyGenerator.PropertyValuePrefix}{i.Id}.{i.VersionString}"))))
            {
                ProjectPropertyElement propertyElement = project.Properties.FirstOrDefault(i => i.Name.Equals(item.Item1));

                propertyElement.ShouldNotBeNull();

                propertyElement.Value.ShouldBe(item.Item2);
            }


            // Verify "before" imports
            //
            for (int i = 0; i < importsBefore.Length; i++)
            {
                ProjectImportElement import = project.Imports.Skip(i).FirstOrDefault();

                import.ShouldNotBeNull();

                import.Project.ShouldBe(importsBefore[i]);

                import.Condition.ShouldBe($" Exists('{importsBefore[i]}') ");
            }

            // Verify "after" imports
            //
            for (int i = 0; i < importsAfter.Length; i++)
            {
                ProjectImportElement import = project.Imports.Skip((project.Imports.Count - importsAfter.Length) + i).FirstOrDefault();

                import.ShouldNotBeNull();

                import.Project.ShouldBe(importsAfter[i]);

                import.Condition.ShouldBe($" Exists('{importsAfter[i]}') ");
            }

            // Verify module extensions were created
            //
            foreach (string item in _moduleExtensions.SelectMany(i => i.Item2))
            {
                string extensionPath = Path.Combine(extensionsPath, item);

                File.Exists(extensionPath).ShouldBeTrue();

                ProjectRootElement extensionProject = ProjectRootElement.Open(extensionPath);

                extensionProject.ShouldNotBeNull();

                extensionProject.Imports.Count.ShouldBe(_packages.Count);

                for (int i = 0; i < _packages.Count; i++)
                {
                    string importProject = $"$({ModulePropertyGenerator.PropertyNamePrefix}{_packages[i].Id.Replace(".", "_")})\\{ModulePropertyGenerator.ImportRelativePath}";
                    ProjectImportElement import = extensionProject.Imports.Skip(i).FirstOrDefault();

                    import.ShouldNotBeNull();

                    import.Project.ShouldBe(importProject);

                    import.Condition.ShouldBe($"Exists('{importProject}')");
                }
            }
        }

        [OneTimeTearDown]
        public void TestCleanup()
        {
            if (Directory.Exists(_intermediateOutputPath))
            {
                Directory.Delete(_intermediateOutputPath, true);
            }
        }

        [OneTimeSetUp]
        public void TestInitialize()
        {
            Directory.CreateDirectory(_packagesPath);

            // Write out a packages.config
            //
            new XDocument(
                new XDeclaration("1.0", "utf8", "yes"),
                new XComment("This file was auto-generated by unit tests"),
                new XElement("packages",
                    _packages.Select(i =>
                        new XElement("package",
                            new XAttribute("id", i.Id),
                            new XAttribute("version", i.VersionString)))
                    )).Save(_packagesConfigPath);

            // Write out a module.config for each module that has one
            //
            foreach (Tuple<string, string[]> moduleExtension in _moduleExtensions)
            {
                PackageInfo package = _packages.First(i => i.Id.Equals(moduleExtension.Item1));

                string moduleConfigPath = Path.Combine(_packagesPath, package.Path, ModulePropertyGenerator.ModuleConfigPath);

                // ReSharper disable once AssignNullToNotNullAttribute
                Directory.CreateDirectory(Path.GetDirectoryName(moduleConfigPath));

                new XDocument(
                    new XDeclaration("1.0", "uf8", "yes"),
                    new XComment("This file was auto-generated by unit tests"),
                    new XElement("configuration",
                        new XElement("extensionImports",
                            moduleExtension.Item2.Select(i => new XElement("add", new XAttribute("name", i)))))
                    ).Save(moduleConfigPath);
            }
        }
    }
}