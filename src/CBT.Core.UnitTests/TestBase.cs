using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CBT.Core.UnitTests
{
    public abstract class TestBase : IDisposable
    {
        private string _testDirectory;

        protected string TestDirectory
        {
            get
            {
                if (_testDirectory == null)
                {
                    _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

                    Directory.CreateDirectory(_testDirectory);
                }

                return _testDirectory;
            }
        }

        protected string TestAssemblyDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        protected string GetFilePath(string filename)
        {
            return Path.Combine(TestDirectory, filename);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Directory.Exists(TestDirectory))
                {
                    Directory.Delete(TestDirectory, recursive: true);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
