using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Construction;

namespace CBT.Core
{
    public sealed class ModulePropertyGenerator
    {
        public bool Generate(string outputPath, string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, string[] importRelativePaths, string[] packageConfigPaths, string[] beforeModuleImports, string[] afterModuleImports)
        {
            if (!CoreUtility.IsFileOutOfDate(outputPath, packageConfigPaths.Concat(new [] { Assembly.GetExecutingAssembly().Location })))
            {
                return true;
            }

            NuGetPropertyGenerator nuGetPropertyGenerator = new NuGetPropertyGenerator();

            ProjectRootElement project = nuGetPropertyGenerator.CreateProjectWithNuGetProperties(propertyNamePrefix, propertyValuePrefix, includeAllVersions, packageConfigPaths);

            foreach(var import in beforeModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
            {
                import.Condition = String.Format(" Exists('{0}') ", import.Project);
            }

            AddImports(project, importRelativePaths, project.Properties.Where(i => i.Name.StartsWith(propertyNamePrefix)).Select(i => String.Format("$({0})", i.Name)));

            foreach (var import in afterModuleImports.Where(i => !String.IsNullOrWhiteSpace(i)).Select(project.AddImport))
            {
                import.Condition = String.Format(" Exists('{0}') ", import.Project);
            }

            project.Save(outputPath);

            return true;
        }

        public bool GenerateModuleImports(string outputPath, string propertyNamePrefix, string propertyValuePrefix, string[] importFiles, string[] importRelativePaths, string[] packageConfigPaths)
        {
            var importPaths = importFiles.Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => Path.Combine(outputPath, i.Trim())).ToList();

            if (!importPaths.Any(i => CoreUtility.IsFileOutOfDate(i, packageConfigPaths)))
            {
                return true;
            }

            NuGetPropertyGenerator nuGetPropertyGenerator = new NuGetPropertyGenerator();

            ProjectRootElement project = nuGetPropertyGenerator.CreateProjectWithNuGetProperties(propertyNamePrefix, propertyValuePrefix, false, packageConfigPaths);

            foreach (string importFile in importPaths)
            {
                var importProject = ProjectRootElement.Create(importFile);

                AddImports(importProject, importRelativePaths, project.Properties.Where(i => i.Name.StartsWith(propertyNamePrefix)).Select(i => String.Format("$({0})", i.Name)));

                importProject.Save();
            }
            return true;
        }

        private void AddImports(ProjectRootElement project, string[] importRelativePaths, IEnumerable<string> modulePaths)
        {
            foreach (var import in modulePaths.SelectMany(modulePath => importRelativePaths.Select(importRelativePath => project.AddImport(Path.Combine(modulePath, importRelativePath)))))
            {
                import.Condition = String.Format("Exists('{0}')", import.Project);
            }
        }
    }

    public static class CoreUtility
    {
        public static bool IsFileOutOfDate(string path, params string[] paths)
        {
            return IsFileOutOfDate(path, paths.AsEnumerable());

        }

        public static bool IsFileOutOfDate(string path, IEnumerable<string> paths)
        {
            if (!File.Exists(path))
            {
                return true;
            }

            DateTime lastWriteTime = File.GetLastWriteTimeUtc(path);

            return paths.FirstOrDefault(i => File.Exists(i) && File.GetLastWriteTimeUtc(i) < lastWriteTime) == null;
        }
    }
}
