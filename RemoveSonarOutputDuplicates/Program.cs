using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RemoveSonarOutputDuplicates
{
    public class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("*** SonarQube output folder missing.");
                return 1;
            }

            var baseDir = args[0];
            if (!Directory.Exists(baseDir))
            { 
                Console.WriteLine("*** Path {0} is invalid!", baseDir);
                return 1;
            }

            Dictionary<string, string> projects = new Dictionary<string, string>();

            foreach (var projectDir in Directory.GetDirectories(baseDir))
            {
                var projectInfoFileName = Path.Combine(projectDir, "ProjectInfo.xml");
                Console.WriteLine("Processing {0}", Path.Combine(new DirectoryInfo(Path.GetDirectoryName(projectInfoFileName)).Name, Path.GetFileName(projectInfoFileName)));
                if (!File.Exists(projectInfoFileName))
                {
                    Console.WriteLine("Not found.", projectInfoFileName);
                    continue;
                }

                XDocument xdoc;
                try
                {
                    xdoc = XDocument.Load(projectInfoFileName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("*** Can't load project info! Message: {1}.", projectInfoFileName, e.ToString());
                    continue;
                }


                string projectGuid = string.Empty;
                try
                {
                    projectGuid = xdoc.Elements()
                        .First((xe) => xe.Name.LocalName == "ProjectInfo").Elements()
                        .First(xe => xe.Name.LocalName == "ProjectGuid").Value;
                    Console.WriteLine("GUID {0}.", projectGuid);
                }
                catch (Exception e)
                {
                    Console.WriteLine("*** Can't load project GUID. Message: {0}", projectInfoFileName, e.ToString());
                    continue;
                }

                if (projects.ContainsKey(projectGuid))
                {
                    try
                    {
                        xdoc.Elements()
                            .First((xe) => xe.Name.LocalName == "ProjectInfo").Elements()
                            .First(xe => xe.Name.LocalName == "IsExcluded").Value = "true";
                        xdoc.Save(projectInfoFileName);
                        Console.WriteLine("Duplicated project removed from analysis!");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("*** Can't set {0} excluded. Message: {1}", projectInfoFileName, e.ToString());
                        return 1;
                    }
                }
                else
                {
                    Console.WriteLine("Nothing to do.");
                    projects[projectGuid] = projectInfoFileName;
                }

                Console.WriteLine();
            }

            return 0;
        }
    }
}
