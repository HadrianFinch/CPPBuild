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

            if ((paramaters[0] != "--help") && (paramaters[0] != "-h") &&
                (paramaters[0] != "/?") && (paramaters[0] != "/h"))
            {
                PrintHeadder();

                Project project = new Project();
                project.Initilize(GetProjectFilePathFromDirectory());

                switch (paramaters[0])
                {
                    case "install":
                    {
                        string moduleName = paramaters[1];

                        if (project.modules.Select((e) => e.name).Contains(moduleName))
                        {
                            Error();
                            Console.WriteLine("A module named \"{0}\" already is installed. To update this module use \"pacman update {0}\"", moduleName);
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

                    case "update":
                    {

                    }
                    break;

                    case "upgrade":
                    {

                    }
                    break;

                    case "init":
                    {

                    }
                    break;

                    case "remove-package":
                    {

                    }
                    break;
                }
            }
            else
            {
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

            return module;
        }

        // returns false if the module already is installed
        public bool DownloadModule(ModuleInfo mod)
        {
            if (!mod.exists)
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C cd " + projectFolder + "\\.\\modules && " + gitCloneString + mod.gitURL;
                process.StartInfo = startInfo;

                process.Start();
                
                // process.WaitForExit();
                int animationCount = 0;
                while (!process.HasExited)
                {
                    // do the animation
                    Console.Write(waitingAnimationString[animationCount % waitingAnimationString.Length]);
                    animationCount++;

                    System.Threading.Thread.Sleep(100);
                    
                    // backspace to delete old character
                    Console.Write("\b");
                }

                return true;
            }
            else
            {
                return false;
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
