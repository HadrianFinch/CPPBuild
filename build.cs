// #define BUILDBUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Runtime.InteropServices;

namespace Build
{
    internal static class BuildApp
    {
        // Build with csc.exe build.cs -define:BUILDBUILD -out:CPPBuild.exe
        #if BUILDBUILD

        static int Main(string[] paramaters)
        {
            bool anyFails = false;
            if ((paramaters.Length > 0) && 
                (paramaters[0] != "--help") && (paramaters[0] != "-h") &&
                (paramaters[0] != "/?") && (paramaters[0] != "/h"))
            {
                Builder.XMLBuildTargets buildInfo = Builder.XMLBuildTargets.Default;
                List<string> targetNames = new List<string>();

                if (paramaters.Length >= 2)
                {
                    buildInfo = GetXMLBuildTargetsFromString(paramaters[1]);
                    if (buildInfo == Builder.XMLBuildTargets.ByName)
                    {
                        if (paramaters.Length > 2)
                        {
                            for (int i = 1; i < paramaters.Length; i++)
                            {
                                if ((paramaters[i].ToCharArray()[0] != '-') &&
                                    (paramaters[i].ToCharArray()[0] != '/'))
                                {
                                    targetNames.Add(paramaters[i]);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            PreBuild(); Error();
                            Console.WriteLine("'name' Requires names to be passed afterwords");
                            Console.ResetColor();
                            return -1;
                        }
                    }
                }

                PreBuild();
                Console.WriteLine("Generating Build Commands...");
                List<BuildCommand> commands = Builder.GenerateBuildCommandsFromXML(paramaters[0], buildInfo, targetNames);
                
                if (commands == null)
                {
                    PreBuild(); Error(); Console.WriteLine("Build commands failed to generate.");
                    Console.ResetColor();
                    return -2;
                }


                foreach (BuildCommand buildTarget in commands)
                {
                    // Compile Resource
                    int retcode = 0;
                    if (!string.IsNullOrEmpty(buildTarget.resourceName))
                    {
                        PreBuild(); Console.WriteLine("Compiling Resource {0}", buildTarget.resourceName);

                        Console.ForegroundColor = ConsoleColor.Red;
                        retcode = buildTarget.CompileResources();
                        
                        PreBuild();
                        string str = "";
                        if (retcode != 0)
                        {
                            str = " with errors";
                            anyFails = true;
                            Error();
                        }
                        Console.WriteLine("Resource compilation finished{1}. rc.exe returned {0}", retcode, str);
                    }

                    Build(); Console.WriteLine("Building {0}", buildTarget.buildName);

                    // In case it prints error text we want it red
                    // Console.ForegroundColor = ConsoleColor.Red;
                    // Nevermind, because it prints the file names

                    try
                    {
                        retcode = buildTarget.Build();
                    }
                    catch (Exception e)
                    {
                        Build(); Error();
                        if (e.Message == "The system cannot find the file specified")
                        {
                            Console.WriteLine("Error runing build command: Could not find cl.exe, make sure it is installed, and either initilize vcvarsall.bat or run build.exe with the -auto switch");
                        }
                        else
                        {
                            Console.WriteLine("Error runing build command: {0}", e.Message);
                        }
                        Console.ResetColor();
                        anyFails = true;
                    }
                    
                    PostBuild();
                    string strer = "";
                    if (retcode != 0)
                    {
                        strer = " with errors";
                        anyFails = true;
                        Error();
                    }
                    Console.WriteLine("Build finished{1}. cl.exe returned {0}", retcode, strer);

                    if (!string.IsNullOrEmpty(buildTarget.manifestCommand))
                    {
                        PostBuild(); Console.WriteLine("Attaching Manifest {0}", buildTarget.manifestName);

                        Console.ForegroundColor = ConsoleColor.Red;
                        retcode = buildTarget.AttachManifest();
                        
                        PostBuild();
                        string str = "";
                        if (retcode != 0)
                        {
                            str = " with errors";
                            anyFails = true;
                            Error();
                        }
                        Console.WriteLine("Manifest attatching finished{1}. mt.exe returned {0}", retcode, str);
                    }
                }
            }
            else
            {
                PrintHelp();
            }
            

            Console.ResetColor();
            if (anyFails)
            {
                return 1;
            }
            return 0;
        }

        private static Builder.XMLBuildTargets GetXMLBuildTargetsFromString(string str)
        {
            Builder.XMLBuildTargets buildInfo = Builder.XMLBuildTargets.Default;
            switch (str)
            {
                case "all":
                {
                    buildInfo = Builder.XMLBuildTargets.All;
                }
                break;

                case "default":
                {
                    buildInfo = Builder.XMLBuildTargets.Default;
                }
                break;

                case "name":
                {
                    buildInfo = Builder.XMLBuildTargets.ByName;
                }
                break;
            }
            return buildInfo;
        }

        private static void PreBuild()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("[Pre Build] ");
        }
        private static void Build()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[Build] ");
        }
        private static void PostBuild()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write("[Post Build] ");
        }
        private static void Error()
        {
            Console.ForegroundColor = ConsoleColor.Red;
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
    

    public class BuildCommand
    {
        [DllImport("msvcrt.dll")]
        private static extern int system(string format);

        public BuildCommand(string name, string command, string manifest, string resource, string resourceName, string manifestName, bool isDefault = false)
        {
            buildName = name;
            buildCommand = command;
            isDefaultBuild = isDefault;
            manifestCommand = manifest;
            resourceCommand = resource;
            this.resourceName = resourceName;
            this.manifestName = manifestName;
        }

        public string buildCommand {get; private set;} = "";
        public string buildName {get; private set;} = "";
        public string manifestCommand {get; private set;} = "";
        public string resourceCommand {get; private set;} = "";
        public string resourceName {get; private set;} = "";
        public string manifestName {get; private set;} = "";

        public bool isDefaultBuild {get; private set;} = false;

        public int Build()
        {
            return system(buildCommand);
        }       

        public int AttachManifest()
        {
            return system(manifestCommand);
        }           

        public int CompileResources()
        {
            return system(resourceCommand);
        }
    }

    public class ModuleInfo
    {
        [DllImport("msvcrt.dll")]
        private static extern int system(string format);

        public string name = "";
        public string gitURL = "";
        public string modfilePath => ("modules\\" + name + "\\module.mod");
        public bool loaded => System.IO.File.Exists(modfilePath);
    }

    public static class Builder
    {
        public enum XMLBuildTargets
        {
            Default,
            All,
            ByName
        }

        private static ModuleInfo ModuleFromXmlElement(XmlElement moduleElm)
        {
            ModuleInfo mod = new ModuleInfo();
            mod.name = moduleElm.GetAttribute("name");
            mod.gitURL = moduleElm.GetAttribute("giturl");
            return mod;
        }

        private static BuildComponents GetBuildComponentsFromXMLElement(XmlElement buildElm)
        {
            BuildComponents bc = new BuildComponents();

            XmlNodeList sourceNodes = buildElm.GetElementsByTagName("source");
            foreach (XmlNode sourceNode in sourceNodes)
            {
                XmlElement sourceElement = sourceNode as XmlElement;
                bc.sources.Add(sourceElement.InnerText);
            }
            
            XmlNodeList defineNodes = buildElm.GetElementsByTagName("define");
            foreach (XmlNode defineNode in defineNodes)
            {
                XmlElement defineElement = defineNode as XmlElement;
                bc.defines.Add(defineElement.InnerText);
            }
            
            XmlNodeList includeNodes = buildElm.GetElementsByTagName("include");
            foreach (XmlNode includeNode in includeNodes)
            {
                XmlElement includeElement = includeNode as XmlElement;
                bc.includes.Add(includeElement.InnerText);
            }

            XmlNodeList outputNodes = buildElm.GetElementsByTagName("output");
            if (outputNodes.Count == 1)
            {
                XmlElement outputElement = outputNodes[0] as XmlElement;
                bc.output = outputElement.InnerText;
            }

            XmlNodeList manifestNodes = buildElm.GetElementsByTagName("manifest");
            if (manifestNodes.Count == 1)
            {
                XmlElement manifestElement = manifestNodes[0] as XmlElement;
                bc.manifest = manifestElement.InnerText;
            }

            XmlNodeList resourceRCNodes = buildElm.GetElementsByTagName("resource");
            if (resourceRCNodes.Count == 1)
            {
                XmlElement resourceRCElement = resourceRCNodes[0] as XmlElement;
                bc.resourceRC = resourceRCElement.InnerText;
            }

            return bc;
        }

        public static List<BuildCommand> GenerateBuildCommandsFromXML(string filepath, XMLBuildTargets buildInfo, List<string> names = null)
        {
            List<BuildCommand> commands = new List<BuildCommand>();

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;

            try
            {
                doc.Load(filepath);
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("The file '" + filepath + "' could not be found. Please make sure you are running this command in the right directory and try again.");
                return null;
            }

            XmlNodeList buildNodes = doc.GetElementsByTagName("build");
            foreach (XmlNode node in buildNodes)
            {
                XmlElement buildElm = node as XmlElement;

                bool isDefault = false;
                if (buildElm.HasAttribute("default"))
                {
                    if (buildElm.GetAttribute("default") == "true")
                    {
                        isDefault = true;
                    }
                }
    
                if (((buildInfo == XMLBuildTargets.Default) && (isDefault)) ||
                    ((buildInfo == XMLBuildTargets.ByName) && (names.Contains(buildElm.GetAttribute("name")))) ||
                    (buildInfo == XMLBuildTargets.All))
                {
                    // Get from XML
                    BuildComponents bcBase = GetBuildComponentsFromXMLElement(buildElm);

                    XmlNodeList modulesNodes = buildElm.GetElementsByTagName("modules");
                    if (modulesNodes.Count == 1)
                    {
                        XmlElement modulesElm = modulesNodes[0] as XmlElement;
                        
                        XmlNodeList moduleNodes = modulesElm.GetElementsByTagName("module");
                        foreach (XmlNode moduleNode in moduleNodes)
                        {
                            XmlElement moduleElm = moduleNode as XmlElement;
                            ModuleInfo mod = ModuleFromXmlElement(moduleElm);

                            XmlDocument modDoc = new XmlDocument();
                            modDoc.PreserveWhitespace = true;

                            try
                            {
                                modDoc.Load(mod.modfilePath);
                            }
                            catch (System.IO.FileNotFoundException)
                            {
                                Console.WriteLine("The mod file '" + mod.modfilePath + "' could not be found. Please make sure this module is installed in the proper directory.");
                                return null;
                            }

                            XmlNodeList containerNodes = modDoc.GetElementsByTagName("module");
                            BuildComponents bc = GetBuildComponentsFromXMLElement(containerNodes[0] as XmlElement);

                            bcBase = bcBase + bc;
                        }
                    }
                    
                    // Generate command
                    string compileCommand = GenerateBuildCommand(bcBase);
                    
                    string manifestCommand = GenerateManifestCommand(bcBase.manifest, bcBase.output);
                    string resourceCommand = GenerateResourceCommand(bcBase.resourceRC);

                    commands.Add(new BuildCommand(buildElm.GetAttribute("name"), compileCommand, manifestCommand, resourceCommand, bcBase.resourceRC, bcBase.manifest, isDefault));
                }
                // else { dont build this target }
            }

            return commands;
        }

        public class BuildComponents
        {
            public List<string> sources = new List<string>();
            public List<string> defines = new List<string>();
            public List<string> includes = new List<string>();
            public string output = "";
            public string resourceRC = "";
            public string manifest = "";

            public static BuildComponents operator+ (BuildComponents a, BuildComponents b)
            {
                BuildComponents c = new BuildComponents();

                c.sources.AddRange(a.sources ); 
                c.sources.AddRange(b.sources);
                c.defines.AddRange(a.defines ); 
                c.defines.AddRange(b.defines);
                c.includes.AddRange(a.includes);
                c.includes.AddRange(b.includes);

                c.output = a.output;
                c.resourceRC = a.resourceRC;
                c.manifest = a.manifest;

                return c;
            }
        }

        public static string GetResourceNameFromRCname(string filepath)
        {
            if (!string.IsNullOrEmpty(filepath))
            {
                return filepath.Substring(0, filepath.Length - 3) + "res";
            }
            return filepath;
        }

        public static string GenerateResourceCommand(string filepath)
        {
            return "rc.exe /nologo " + filepath;
        }

        public static string GenerateManifestCommand(string filepath, string targetExePath)
        {
            if (!string.IsNullOrEmpty(filepath))
            {
                return "mt.exe -manifest " + filepath + " -outputresource:" + targetExePath + ";1";
            }
            return filepath;
        }

        public static string GenerateBuildCommand(BuildComponents bc)
        {
            string buildstring = "cl.exe /W0 /Zi /EHsc /std:c++17 /nologo ";
            
            foreach (string file in bc.sources)
            {
                buildstring += (file + " ");
            }

            foreach (string define in bc.defines)
            {
                buildstring += ("/D" + define + " ");
            }

            foreach (string include in bc.includes)
            {
                buildstring += ("/I" + include + " ");
            }

            if (!string.IsNullOrEmpty(bc.output))
            {
                buildstring += ("/Fe: " + bc.output + " ");
            }

            if (!string.IsNullOrEmpty(GetResourceNameFromRCname(bc.resourceRC)))
            {
                buildstring += "/link /SUBSYSTEM:WINDOWS " + GetResourceNameFromRCname(bc.resourceRC);
            }

            return buildstring;
        }
    }
}