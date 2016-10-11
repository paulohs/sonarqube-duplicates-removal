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
    public class ProjectInfo
    {
        public string File { get; set; }
        public string Guid { get; set; }
        public bool HasRoslynReport { get; set; }
    }

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
            List<ProjectInfo> projectsAll = new List<ProjectInfo>();

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

                /*
                xdoc.Elements()
                    .First((xe) => xe.Name.LocalName == "ProjectInfo").Elements()
                    .First(xe => xe.Name.LocalName == "IsExcluded").Value = "false";
                xdoc.Save(projectInfoFileName);
                */

                string projectGuid = string.Empty;
                try
                {
                    projectGuid = xdoc.Elements()
                        .First((xe) => xe.Name.LocalName == "ProjectInfo").Elements()
                        .First(xe => xe.Name.LocalName == "ProjectGuid").Value;
                    Console.WriteLine("GUID {0}", projectGuid);
                }
                catch (Exception e)
                {
                    Console.WriteLine("*** Can't load project GUID. Message: {0}", projectInfoFileName, e.ToString());
                    continue;
                }

                bool hasRoslynReportFilePath = false;
                try
                {
                    hasRoslynReportFilePath = xdoc.Elements()
                        .First((xe) => xe.Name.LocalName == "ProjectInfo").Elements()
                        .First(xe => xe.Name.LocalName == "AnalysisSettings").Elements()
                        .Any((xe) => xe.Attributes().Any((xa) => xa.Name.LocalName == "Name" && xa.Value == "sonar.cs.roslyn.reportFilePath"));
                    Console.WriteLine("Roslyn Report File Path: {0}", hasRoslynReportFilePath);
                }
                catch (Exception e)
                {
                    Console.WriteLine("*** Can't load project sonar.cs.roslyn.reportFilePath attribute. Message: {0}", projectInfoFileName, e.ToString());
                    continue;
                }

                projectsAll.Add(new ProjectInfo() { File = projectInfoFileName, Guid = projectGuid, HasRoslynReport = hasRoslynReportFilePath });
               
                Console.WriteLine();
            }

            Console.WriteLine("Verifying duplicated projects...");
            foreach (string guid in projectsAll.Select((pi) => pi.Guid).Distinct())
            {
                var duplicatedProjs = projectsAll.Where((pi) => pi.Guid == guid).ToList();
                
                if (duplicatedProjs.Count > 1)
                {
                    Console.WriteLine("There are {0} projects with GUID {1}.", duplicatedProjs.Count, guid);

                    var projWithRoslyn = duplicatedProjs.FirstOrDefault((pi) => pi.HasRoslynReport);

                    bool thereIsAWinner = false;
                    for (int i = 0; i < duplicatedProjs.Count; i++)
                    {
                        if (projWithRoslyn != null && duplicatedProjs[i].File == projWithRoslyn.File)
                        {
                            Console.WriteLine("Project {0} with Roslyn report will be preserved!", duplicatedProjs[i].File);
                            thereIsAWinner = true;
                            continue;
                        }

                        if (i == (duplicatedProjs.Count - 1) && !thereIsAWinner)
                        {
                            Console.WriteLine("Remaining project {0} will be preserved!", duplicatedProjs[i].File);
                            continue;
                        }
                        else
                        {
                            try
                            {
                                var xdoc = XDocument.Load(duplicatedProjs[i].File);
                                xdoc.Elements()
                                    .First((xe) => xe.Name.LocalName == "ProjectInfo").Elements()
                                    .First(xe => xe.Name.LocalName == "IsExcluded").Value = "true";
                                xdoc.Save(duplicatedProjs[i].File);
                                Console.WriteLine("Duplicated project {0} removed from analysis!", duplicatedProjs[i].File);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("*** Can't set {0} excluded. Message: {1}", duplicatedProjs[i].File, e.ToString());
                                return 1;
                            }
                        }
                    }
                    Console.WriteLine();
                }
            }

            return 0;
        }
    }
}
