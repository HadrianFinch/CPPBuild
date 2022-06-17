// by the way, pacman stands for Package Manager
#define PACMANBUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Text;
using System.Net;
using System.Linq;


namespace PacMan
{
    internal static class PacManApp
    {
        #if PACMANBUILD

        static int Main(string[] paramaters)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            bool invalidCommand = false;

            if ((paramaters[0] != "--help") && (paramaters[0] != "-h") &&
                (paramaters[0] != "/?") && (paramaters[0] != "/h"))
            {
                PrintHeadder();

                Project project = null;
                if (paramaters[0] != "init")
                {
                    project = new Project();
                    project.Initilize(GetProjectFilePathFromDirectory());
                }

                switch (paramaters[0])
                {
                    case "install":
                    {
                        string moduleName = paramaters[1];

                        if (project.modules.Select((e) => e.name).Contains(moduleName))
                        {
                            Error();
                            Console.WriteLine("A module named \"{0}\" already is installed. To update this module use \"pacman update {1}\"", moduleName, moduleName);
                            invalidCommand = true;
                            break;
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[installer] Installing module {0} and adding it to project\n[installer] Fetching module details...", moduleName);

                        Project.ModuleInfo mod = null;
                        try
                        {
                            mod = project.AddModule(moduleName);
                        }
                        catch (FileNotFoundException e)
                        {
                            Error(); Console.WriteLine(e.Message);
                            break;
                        }

                        Console.WriteLine("[installer] Module details sucessfully fetched and added to project.\n[installer] Downloading module {0}...", moduleName);

                        project.DownloadModule(mod);

                        Console.WriteLine("[installer] Module download completed, and sucessfully added to project");
                    }
                    break;

                    // case "update":
                    // {
                    //     List<Project.ModuleInfo> modulesToUpdate = new List<Project.ModuleInfo>();
                    //     if (paramaters.Length > 1)
                    //     {
                    //         for (int i = 1; i < paramaters.Length; i++)
                    //         {
                    //             string name = paramaters[i];
                    //             Project.ModuleInfo mod = project.GetModuleByName(name);

                    //             if (mod == null)
                    //             {
                    //                 Error();
                    //                 Console.WriteLine("No installed module with name {0} could be found!", name);
                    //                 invalidCommand = true;
                    //                 break;
                    //             }

                    //             modulesToUpdate.Add(mod);
                    //         }
                    //     }
                    //     else
                    //     {

                    //     }
                    // }
                    // break;

                    // case "upgrade":
                    // {

                    // }
                    // break;

                    case "status":
                    {
                        // It just needs to not default in order to print the status
                    }
                    break;

                    case "init":
                    {
                        string appName;
                        string manifest;
                        string resource;
                        List<string> defines;
                        List<string> includes;

                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.Write("[project setup] Setting up folder for a new project\n[project setup] What is the name of the app: ");
                        appName = Console.ReadLine();

                        Console.Write("[project setup] [build setup] Manifest file (hit \"enter\" for none): ");
                        manifest = Console.ReadLine();

                        Console.Write("[project setup] [build setup] resource file (hit \"enter\" for none): ");
                        resource = Console.ReadLine();

                        Console.Write("[project setup] [build setup] compile-time #defines (seprated by spaces) (hit \"enter\" for none): ");
                        defines = (Console.ReadLine().Split(' ').ToList());

                        Console.Write("[project setup] [build setup] list of folders to add to include path (seprated by spaces) (hit \"enter\" for none): ");
                        includes = (Console.ReadLine().Split(' ').ToList());

                        Console.Write("[project setup] Generatiing project files");

                        
                        XmlDocument doc = new XmlDocument();
                        doc.PreserveWhitespace = true;

                        string s = string.Format("<project>\n    <build name=\"{0}\" default=\"true\">\n        <source>*.cpp</source>\n        <output>bin\\{1}.exe</output>\n        </build>\n\n    <modules>\n        \n    </modules>\n</project>", appName, appName);
                        doc.LoadXml(s);

                        XmlElement buildContainer = doc.GetElementsByTagName("build")[0] as XmlElement;

                        if (!string.IsNullOrEmpty(manifest))
                        {
                            XmlNode node = doc.CreateNode("element", "manifest", "");
                            node.InnerText = manifest;

                            buildContainer.AppendChild(node);
                        }

                        if (!string.IsNullOrEmpty(resource))
                        {
                            XmlNode node = doc.CreateNode("element", "resource", "");
                            node.InnerText = resource;

                            buildContainer.AppendChild(node);
                        }

                        foreach (string str in defines)
                        {
                            XmlNode node = doc.CreateNode("element", "define", "");
                            node.InnerText = str;

                            buildContainer.AppendChild(node);
                        }

                        foreach (string str in includes)
                        {
                            XmlNode node = doc.CreateNode("element", "include", "");
                            node.InnerText = str;

                            buildContainer.AppendChild(node);
                        }

                        doc.Save(".\\project.proj");

                        DirectoryInfo modulesDirInfo = Directory.CreateDirectory(".\\modules");

                        foreach (FileInfo file in modulesDirInfo.GetFiles())
                        {
                            file.Delete(); 
                        }
                        foreach (DirectoryInfo dir in modulesDirInfo.GetDirectories())
                        {
                            dir.Delete(true); 
                        }

                        Directory.CreateDirectory(".\\bin");

                        project = new Project();
                        project.Initilize(GetProjectFilePathFromDirectory());
                    }
                    break;

                    case "remove-module":
                    {
                        string moduleName = paramaters[1];
                        
                        if (!project.modules.Select((e) => e.name).Contains(moduleName))
                        {
                            Error();
                            Console.WriteLine("No installed module named \"{0}\" could be found. To install this module use \"pacman install {1}\"", moduleName, moduleName);
                            invalidCommand = true;
                            break;
                        }

                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("[uninstaller] Preparing to uninstall {0}\n[uninstaller] Fetching module details...\n[uninstaller] Removing module files...", moduleName);

                        Project.ModuleInfo mod = project.GetModuleByName(moduleName);

                        Console.WriteLine("[uninstaller] Module details found, deleting module files");

                        project.DeleteModule(mod);

                        Console.WriteLine("[uninstaller] Sucessfully removed all module files\n[uninstaller] Removing module from project...");

                        project.RemoveModule(moduleName);

                        Console.WriteLine("[uninstaller] Sucessfully removed module from project");
                    }
                    break;

                    default:
                    {
                        invalidCommand = true;
                    }
                    break;
                }

                if (!invalidCommand)
                {
                    Console.Write("\n");                
                    PrintStatus(project);
                }
            }
            else
            {
                invalidCommand = true;
                PrintHelp();
            }

            Console.ResetColor();
            return 0;
        }

        private static string GetProjectFilePathFromDirectory()
        {
            string[] strings = Directory.GetFiles(".", "*.proj");
            return strings[0];
        }

        private static void Error()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[error] ");
        }

        private static void PrintHeadder()
        {
            Console.WriteLine("PacMan Package Manager v1.0.0");
        }

        private static void PrintStatus(Project project)
        {
            Console.ResetColor();
            Console.WriteLine("*-----------------------STATUS-----------------------*");

            Console.Write    ("|");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write     (" Modules Added: [{0}]       ", project.status.modulesAdded);
            Console.ResetColor();
            Console.Write                                ("|");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write                              ("    Modules Removed: [{0}] ", project.status.modulesRemoved);
            Console.ResetColor();
            Console.Write                                                         ("|\n");

            Console.Write    ("|");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write     (" Modules Downloaded: [{0}]  ", project.status.modulesDownloaded);
            Console.ResetColor();
            Console.Write                                ("|");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write                              ("    Modules Deleted: [{0}] ", project.status.modulesDeleted);
            Console.ResetColor();
            Console.Write                                                         ("|\n");

            // Console.WriteLine("|                          |                         |");
            Console.WriteLine("|--------------------------*-------------------------|");
            // Console.WriteLine("|                          |                         |");

            Console.Write    ("|");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write     (" Modules Changed: [{0}]     ", project.status.modulesChanged);
            Console.ResetColor();
            Console.Write                                ("|");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write                              ("    Modules Updated: [{0}] ", project.status.modulesUpdated);
            Console.ResetColor();
            Console.Write                                                         ("|\n");

            Console.Write    ("|");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write     (" Modules With Errors: [{0}] ", project.status.modulesWithErrors);
            Console.ResetColor();
            Console.Write                                ("|");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write                              ("      Total Modules: [{0}] ", project.modules.Count);
            Console.ResetColor();
            Console.Write                                                         ("|\n");

            Console.WriteLine("*----------------------------------------------------*");
        }

        private static void PrintHelp()
        {
            Console.ResetColor();
            Console.WriteLine("------------------------USAGE------------------------");
            Console.WriteLine("  CPPBuild.exe [buildscript] [optional:build specifyer] [optional:...]");
            Console.WriteLine("    buildscript: the local or complete path to the buildscript file");
            Console.WriteLine("    build specifyer: which build targets to build. can be");
            Console.WriteLine("      default: builds the target marked with default=\"true\"");
            Console.WriteLine("      all: builds all targets in the buildscript");
            Console.WriteLine("      name [name][...]: builds the targets with the names specifyed");
        }
        #endif

    }

    public class Project
    {
        public class ModuleInfo
        {
            public string name = "";
            public string gitURL = "";
            public string modfilePath => ("modules\\" + name + "\\module.mod");
            public bool exists => System.IO.File.Exists(modfilePath);
        }

        public class Status
        {
            public int modulesAdded = 0;
            public int modulesDownloaded = 0;
            public int modulesRemoved = 0;
            public int modulesDeleted = 0;
            public int modulesChanged = 0;
            public int modulesUpdated = 0;
            public int modulesWithErrors = 0;

            public Status()
            {
            }
        }

        public const string githubQuerryString = "https://github.com/topics/pacman-package?q=";
        public const string gitUrlSeed = "https://github.com/";
        public const string gitCloneString = "git clone --quiet ";
        public const string gitHtmlSearchStringStart = "            ";
        public const string gitHtmlSearchString = "<a data-hydro-click=\"{&quot;event_type&quot;:&quot;explore.click&quot;,&quot;payload&quot;:{&quot;click_context&quot;:&quot;REPOSITORY_CARD&quot;,&quot;click_target&quot;:&quot;OWNER";
        public const string waitingAnimationString = "\\|/-";

        public string projFilePath {get; internal set;} = "";
        public string projectFolder {get; internal set;} = "";
        public bool initilized {get; private set;} = false;
        public XmlDocument projXml {get; private set;} = null;

        public Status status {get; private set;} = new Status();

        public List<ModuleInfo> modules = new List<ModuleInfo>();

        public void Initilize(string projFilePath)
        {
            if (File.Exists(projFilePath))
            {
                this.projFilePath = projFilePath;
                this.projectFolder = new FileInfo(projFilePath).Directory.FullName;
            }
            else
            {
                throw new FileNotFoundException("The file " + projFilePath + " does not exist.");
            }

            projXml = new XmlDocument();
            projXml.PreserveWhitespace = true;

            projXml.Load(projFilePath);

            XmlNodeList moduleNodes = projXml.GetElementsByTagName("module");

            foreach (XmlNode node in moduleNodes)
            {
                XmlElement element = node as XmlElement;

                ModuleInfo module = ModuleInfoFromName(element.InnerText);

                modules.Add(module);
            }
        }

        public ModuleInfo GetModuleByName(string name)
        {
            int index = modules.Select((m) => m.name).ToList().IndexOf(name);
            if (index == -1)
            {
                return null;
            }
            else
            {
                return modules[index];
            }
        }

        private ModuleInfo ModuleInfoFromName(string name)
        {
            ModuleInfo module = new ModuleInfo();

            module.name = name;
            module.gitURL = GetGitUrlFromModuleName(module.name);

            return module;
        }

        public ModuleInfo AddModule(string name)
        {
            ModuleInfo module = ModuleInfoFromName(name);
            modules.Add(module);

            XmlNode newModNode = projXml.CreateNode("element", "module", "");
            newModNode.InnerText = module.name;

            XmlElement modulesContainerNode = projXml.GetElementsByTagName("modules")[0] as XmlElement;
            modulesContainerNode.AppendChild(newModNode);
            
            projXml.Save(projFilePath);

            status.modulesAdded++;
            status.modulesChanged++;

            return module;
        }

        public bool RemoveModule(string modName)
        {
            XmlNodeList modulesNodes = projXml.GetElementsByTagName("module");

            bool removed = false;
            foreach (XmlNode node in modulesNodes)
            {
                if (node.InnerText == modName)
                {
                    removed = true;
                    node.ParentNode.RemoveChild(node);

                    modules.Remove(modules[modules.Select((m) => m.name).ToList().IndexOf(modName)]);

                    projXml.Save(projFilePath);
                    break;
                }
            }

            status.modulesRemoved++;
            status.modulesChanged++;

            return removed;
        }

        public void DeleteModule(ModuleInfo mod)
        {
            if (mod.exists)
            {                
                // remove the files
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C cd " + projectFolder + "\\.\\modules && rd /s /q " + mod.name;
                process.StartInfo = startInfo;

                process.Start();
                
                // process.WaitForExit();
                WaitLoader(() => process.HasExited);

                status.modulesDeleted++;
            }
            else
            {
                throw new FileNotFoundException("Could not find the module files.");
            }
        }

        private void WaitLoader(Func<bool> check)
        {
            int animationCount = 0;
            while (check() == false)
            {
                // do the animation
                Console.Write(waitingAnimationString[animationCount % waitingAnimationString.Length]);
                animationCount++;

                System.Threading.Thread.Sleep(100);
                
                // backspace to delete old character
                Console.Write("\b");
            }
        }

        // returns false if the module already is installed
        public bool DownloadModule(ModuleInfo mod)
        {
            if (!mod.exists)
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C cd " + projectFolder + "\\.\\modules && " + gitCloneString + mod.gitURL;
                process.StartInfo = startInfo;

                process.Start();
                
                // process.WaitForExit();
                WaitLoader(() => process.HasExited);

                status.modulesDownloaded++;

                return true;
            }
            else
            {
                return false;
            }
        }

        public void UpdateModule(ModuleInfo mod)
        {
            if (true)
            {
                
            }
        }

        public static string GetGitUrlFromModuleName(string name)
        {
            string html = GetHtmlResponseFromUrl(githubQuerryString + name);

            int elementStartPos = html.IndexOf(gitHtmlSearchString);

            if (elementStartPos == -1)
            {
                throw new FileNotFoundException("No modules found with name " + name + " could be found. Make sure you have the correct name.");
            }

            int startPos = (html.IndexOf(gitHtmlSearchStringStart, elementStartPos) + gitHtmlSearchStringStart.Length);

            int endPos = html.IndexOf("\n</a>", startPos);

            string username = html.Substring(startPos, endPos - startPos);

            string url = gitUrlSeed + username + "/" + name + ".git";

            return url;
        }

        internal static string GetHtmlResponseFromUrl(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            // Set some reasonable limits on resources used by this request
            request.MaximumAutomaticRedirections = 4;
            request.MaximumResponseHeadersLength = 4;

            // Set credentials to use for this request.
            request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Get the stream associated with the response.
            Stream receiveStream = response.GetResponseStream ();

            // Pipes the stream to a higher level stream reader with the required encoding format.
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);

            string str = readStream.ReadToEnd();

            response.Close();
            readStream.Close();

            return str;
        }
    }
}
