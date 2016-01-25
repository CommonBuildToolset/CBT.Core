using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CBT.Core.Internal;
using CBT.Core.Tasks;
using Microsoft.Build.Construction;

namespace CBT.Core
{
    public sealed class ModulePropertyGenerator
    {
        public bool Generate(string outputPath, string propertyNamePrefix, string propertyValuePrefix, bool includeAllVersions, string[] importRelativePaths, string[] packageConfigPaths, string[] beforeModuleImports, string[] afterModuleImports)
        {
            ProjectRootElement project = GenerateNuGetProperties.CreateProjectWithNuGetProperties(propertyNamePrefix, propertyValuePrefix, includeAllVersions, packageConfigPaths);

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

        public bool GenerateModuleImports(string outputPath, string propertyNamePrefix, string propertyValuePrefix, string[] importFiles, string[] importRelativePaths, string[] packageConfigPaths, string[] moduleInputs)
        {
            var importPaths = importFiles.Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => Path.Combine(outputPath, i.Trim())).ToList();

            if (!importPaths.Any(i => CoreUtility.IsFileOutOfDate(i, moduleInputs)))
            {
                return true;
            }

            ProjectRootElement project = GenerateNuGetProperties.CreateProjectWithNuGetProperties(propertyNamePrefix, propertyValuePrefix, false, packageConfigPaths);

            foreach (string importFile in importPaths)
            {
                var importProject = ProjectRootElement.Create(importFile);

                AddImports(importProject, importRelativePaths, project.Properties.Where(i => i.Name.StartsWith(propertyNamePrefix)).Select(i => String.Format("$({0})", i.Name)));

                importProject.Save();
            }
            return true;
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
