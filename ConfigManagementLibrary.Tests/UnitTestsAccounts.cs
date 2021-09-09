using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using Daniels.Config;

namespace Daniels.Config.Tests
{
    class UnitTestsConfigManagement
    {
        [Test]
        public async Task TestMakeLookupFileName()
        {
            string lookupFileName = ConfigManagement.MakeLookupFileName(1, "testConfig");
            Console.WriteLine("LookupFileName: {0}", lookupFileName);
        }
    }
}
