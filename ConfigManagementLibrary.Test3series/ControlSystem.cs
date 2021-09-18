using System;
using System.Text;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharp.Reflection;

using Daniels.Config;

namespace Daniels.Config.Test
{
    public class ControlSystem : CrestronControlSystem
    {
        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// 
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        /// 
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 100;

                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);

                CrestronConsole.AddNewConsoleCommand(ConsoleCommandConfigInit, "configtestinit", "test ConfigManagementLibrary init handling", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(ConsoleCommandConfigPath, "configtestpath", "test ConfigManagementLibrary path handling", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(ConsoleCommandConfigLoad, "configtestload", "test ConfigManagementLibrary load merged object handling", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(ConsoleCommandTest, "test", "test", ConsoleAccessLevelEnum.AccessOperator);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor 
        /// has finished. 
        /// 
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        /// 
        /// Please be aware that InitializeSystem needs to exit quickly also; 
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
            try
            {

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down. 
        /// Use these events to close / re-open sockets, etc. 
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values 
        /// such as whether it's a Link Up or Link Down event. It will also indicate 
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType"></param>
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType"></param>
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }

        /// <summary>
        /// Test Console functions.
        /// </summary>
        /// <param name="cmd">command name</param>
        private void ConsoleCommandConfigInit(string cmd)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                CrestronConsole.PrintLine("Config Pre:{0}", ConfigManagement.DefaultConfig);
                ConfigManagement.InitializeLibraryDefaults();
                CrestronConsole.PrintLine("Config Post:{0}", ConfigManagement.DefaultConfig);
                CrestronConsole.ConsoleCommandResponse("Config pattern: {0}", ConfigManagement.DefaultConfig.DefaultConfigFileNamePattern);
            }
            catch (Exception e)
            {
                CrestronConsole.ConsoleCommandResponse("Error: {0}\r\n{1}", e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Test Console functions.
        /// </summary>
        /// <param name="cmd">command name</param>
        private void ConsoleCommandConfigPath(string cmd)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.AppendFormat("Config:");
                sb.AppendLine();
                sb.Append(ConfigManagement.DefaultConfig);
                sb.AppendLine();
                string[] lookupPath = ConfigManagement.LookUpConfigs("testConfig", null, InitialParametersClass.ApplicationNumber);
                foreach (string folder in lookupPath)
                {
                    sb.AppendFormat("\t{0}", folder);
                    sb.AppendLine();
                }
                CrestronConsole.ConsoleCommandResponse("LookupFiles: \r\n{0}", sb.ToString());
            }
            catch (Exception e)
            {
                CrestronConsole.ConsoleCommandResponse("Error: {0}\r\n{1}\r\nAccumulated output\r\n{2}", e.Message, e.StackTrace, sb.ToString());
            }
        }

        /// <summary>
        /// Test Console functions.
        /// </summary>
        /// <param name="cmd">command name</param>
        private void ConsoleCommandConfigLoad(string cmd)
        {
            try
            {
                TestConfig testConfig = ConfigManagement.LoadMergedJsonConfig<TestConfig>("testConfig", null, 0x01);
                CrestronConsole.ConsoleCommandResponse("Loaded config: \r\n{0}", testConfig);
            }
            catch (Exception e)
            {
                CrestronConsole.ConsoleCommandResponse("Error: {0}\r\n{1}", e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Test Console functions.
        /// </summary>
        /// <param name="cmd">command name</param>
        private void ConsoleCommandTest(string cmd)
        {
            try
            {
                CrestronConsole.PrintLine("Line:{0}", 1);
                Assembly a = Assembly.GetExecutingAssembly();
                CrestronConsole.PrintLine("Line:{0}", 2);
                AssemblyName assemblyName = a.GetName();
                CrestronConsole.PrintLine("Line:{0}", 3);
                string codeBase = assemblyName.CodeBase;
                CrestronConsole.PrintLine("Line:{0}", 4);
                Uri uri = new Uri(codeBase);
                CrestronConsole.PrintLine("Line:{0}", 5);
                string path = Uri.UnescapeDataString(uri.LocalPath);
                CrestronConsole.PrintLine("Line:{0}", 6);
                CrestronConsole.PrintLine("path:{0}", path);
                CrestronConsole.PrintLine("program:{0}", InitialParametersClass.ProgramDirectory.ToString());
            }
            catch (Exception e)
            {
                CrestronConsole.ConsoleCommandResponse("Error: {0}\r\n{1}", e.Message, e.StackTrace);
            }
        }
    }
}