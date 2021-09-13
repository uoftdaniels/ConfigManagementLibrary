using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace Daniels.Config.Test
{
    public class TestConfig
    {
        public string TestGlobal;
        public string TestGlobalOverrided;
        public string TestLocal;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("TestGlobal: {0}", TestGlobal);
            sb.AppendLine();
            sb.AppendFormat("TestGlobalOverrided: {0}", TestGlobalOverrided);
            sb.AppendLine();
            sb.AppendFormat("TestLocal: {0}", TestLocal);
            sb.AppendLine();
            return sb.ToString();
        }
    }
}