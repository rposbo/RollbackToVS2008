using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using RollbackToVS2008.Interfaces;

namespace RollbackToVS2008.Implementations
{
    class VsFileManipulation : IFileManipulation
    {
        public IVersionControl VersionControl { get; set; }
        public string WorkspaceRoot { get; set; }
        public Direction ToggleDirection { get; set; }
        
        public VsFileManipulation(IVersionControl versionControl, string workspaceRoot, Direction toggleDirection)
        {
            VersionControl = versionControl;
            WorkspaceRoot = workspaceRoot;
            ToggleDirection = toggleDirection;
        }

        public void ProcessFiles()
        {
            ProcessSolutionFiles();
            ProcessProjFiles();
        }

        public void ProcessSolutionFiles()
        {
            // what files to open
            const string fileTypeToOpen = "*.sln";


            // get all the files we want and loop through them
            foreach (var file in Directory.GetFiles(WorkspaceRoot
                                                , fileTypeToOpen
                                                , SearchOption.AllDirectories))
            {
                // open
                var originalContents = File.ReadAllText(file);
                var contents = originalContents;

                // find and replace
                foreach (var match in _patternsToMatch)
                {
                    contents = (ToggleDirection == Direction.From2010To2008)
                                   ? new Regex(match.Key).Replace(contents, match.Value)
                                   : new Regex(match.Value).Replace(contents, match.Key);
                }

                if (!originalContents.Equals(contents))
                {
                    File.WriteAllText(file + ".bak", originalContents, Encoding.UTF8);

                    VersionControl.CheckOutFile(file);

                    File.WriteAllText(file, contents, Encoding.UTF8);
                }
            }
        }

        public void ProcessProjFiles()
        {
            // what files to open
            var fileTypesToOpen = new [] {"*.csproj", "*.vbproj"};
            
            // get all the files we want and loop through them
            foreach(var fileTypeToOpen in fileTypesToOpen)
                foreach (var file in Directory.GetFiles(WorkspaceRoot
                                                , fileTypeToOpen
                                                , SearchOption.AllDirectories))
            {
                // open
                var projFile = XDocument.Load(file);
                var originalProjFile = XDocument.Load(file);

                if (projFile.Root != null)
                {
                    // change ToolsVersion attribute
                    projFile.Root.SetAttributeValue("ToolsVersion",((ToggleDirection == Direction.From2010To2008) ? "3.5" : "4.0"));

                    if (ToggleDirection == Direction.From2010To2008)
                    {
                        // change VS2010 elements
                        foreach (var vs2010ElementSet in _VS2010Elements)
                        {
                            foreach (var vs2010Element in vs2010ElementSet.Item4)
                            {
                                if (vs2010Element.Name.LocalName == "ItemGroup")
                                {
                                    foreach (var vs2010ItemGroupElement in vs2010Element.Elements())
                                    projFile.Root.Descendants().Where(
                                        e => e.Name.LocalName == vs2010ItemGroupElement.Name.LocalName)
                                        .Remove();
                                }
                                else
                                {
                                    projFile.Root.Descendants().Where(
                                        e => e.Name.LocalName == vs2010Element.Name.LocalName)
                                        .Remove();
                                }
                            }
                        }

                        // remove the potentially now empty ItemGroup element
                        projFile.Root.Descendants().Where(e => e.Name.LocalName == "ItemGroup" && e.Nodes().Count() == 0).
                            Remove();
                    }
                    else
                    {
                            projFile.Root.Descendants().Where(e => e.Name.LocalName =="OldToolsVersion").Remove();

                        // change VS2010 elements
                        foreach (var vs2010ElementSet in _VS2010Elements)
                        {

                            var parentElement = vs2010ElementSet.Item1;
                            var parentElementAttribute = vs2010ElementSet.Item2;
                            var parentElementAttributeValue = vs2010ElementSet.Item3;

                            if (parentElement == "PropertyGroup" && parentElementAttribute == "")
                            {
                                var position =
                                    projFile.Root.Descendants().Where(e => e.Name.LocalName == "PropertyGroup").First();

                                foreach (var vs2010Element in vs2010ElementSet.Item4)
                                    position.Add(vs2010Element);
                            }
                            else if (parentElement == "PropertyGroup" && parentElementAttribute == "Condition")
                            {
                                var position =
                                    projFile.Root.Descendants().Where(e => e.Name.LocalName == "PropertyGroup").Where(
                                        e =>
                                        e.Attributes().Where(
                                            a =>
                                            a.Name.LocalName == parentElementAttribute &&
                                            a.Value.Contains(parentElementAttributeValue)).Any());

                                foreach (var elem in position)
                                {
                                    foreach (var vs2010Element in vs2010ElementSet.Item4)
                                        elem.Add(vs2010Element);
                                }
                            }
                            else if (parentElement == "")
                            {
                                var position = projFile.Root.Descendants().Where(e => e.Name.LocalName == "ItemGroup").Last();

                                foreach (var vs2010Element in vs2010ElementSet.Item4)
                                    position.AddAfterSelf(vs2010Element);
                                
                            }

                        }
                    }
                }

                // has the file actually changed? if not, undo checkout, else save file
                if (originalProjFile.ToString() != projFile.ToString())
                {
                    File.WriteAllText(file + ".bak",
                                      @"<?xml version=""1.0"" encoding=""utf-8""?>" + Environment.NewLine +
                                      originalProjFile.ToString().Replace(@" xmlns=""""", string.Empty), Encoding.UTF8);

                    VersionControl.CheckOutFile(file);

                    File.WriteAllText(file,
                                      @"<?xml version=""1.0"" encoding=""utf-8""?>" + Environment.NewLine +
                                      projFile.ToString().Replace(@" xmlns=""""", string.Empty), Encoding.UTF8);
                }
            }
        }

        public int Commit(string checkInComment)
        {
            var changeset = VersionControl.CheckInAll(checkInComment);
            return changeset;
        }

        public int Rollback()
        {
            var rollbackStatus = VersionControl.UndoCheckOutAll();
            return rollbackStatus;
        }

        public enum Direction
        {
            From2008To2010,
            From2010To2008
        }

        private Dictionary<string, string> _patternsToMatch = new Dictionary<string, string>
                                                                  {
                                                                      {
                                                                          "Microsoft Visual Studio Solution File, Format Version 11.00"
                                                                          ,
                                                                          "Microsoft Visual Studio Solution File, Format Version 10.00"
                                                                          },
                                                                      {
                                                                          "# Visual Studio 2010",
                                                                          "# Visual Studio 2008"
                                                                          }
                                                                  };

        private Tuple<string, string, string, XElement[]>[] _VS2010Elements = new[]
                                                                                  {
                                                                                      new Tuple
                                                                                          <string, string, string,
                                                                                          XElement[]>(
                                                                                          "PropertyGroup", "", "", new[]
                                                                                                                       {
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "FileUpgradeFlags", Environment.NewLine + "    ")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "OldToolsVersion",
                                                                                                                               "4.0")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "UpgradeBackupLocation")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "PublishUrl",
                                                                                                                               @"publish\")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "Install",
                                                                                                                               "true")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "InstallFrom",
                                                                                                                               "Disk")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "UpdateEnabled",
                                                                                                                               "false")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "UpdateMode",
                                                                                                                               "Foreground")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "UpdateInterval",
                                                                                                                               7)
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "UpdateIntervalUnits",
                                                                                                                               "Days")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "UpdatePeriodically",
                                                                                                                               "false")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "UpdateRequired",
                                                                                                                               "false")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "MapFileExtensions",
                                                                                                                               "true")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "ApplicationRevision",
                                                                                                                               0)
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "ApplicationVersion",
                                                                                                                               "1.0.0.%2a")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "IsWebBootstrapper",
                                                                                                                               "false")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "UseApplicationTrust",
                                                                                                                               "false")
                                                                                                                           ,
                                                                                                                           new XElement
                                                                                                                               (
                                                                                                                               "BootstrapperEnabled",
                                                                                                                               "true")
                                                                                                                       }
                                                                                          ),
                                                                                      new Tuple
                                                                                          <string, string, string,
                                                                                          XElement[]>(
                                                                                          "PropertyGroup", "Condition",
                                                                                          " '$(Configuration)|$(Platform)'"
                                                                                          ,
                                                                                          new[]
                                                                                              {
                                                                                                  new XElement(
                                                                                                      "CodeAnalysisRuleSet",
                                                                                                      "AllRules.ruleset")
                                                                                              }
                                                                                          )
                                                                                      ,
                                                                                      new Tuple
                                                                                          <string, string, string,
                                                                                          XElement[]>(
                                                                                          "", "", "", new[]
                                                                                                                   {
                                                                                                                       new XElement
                                                                                                                           ("ItemGroup",
                                                                                                                            new XElement
                                                                                                                                ("BootstrapperPackage",
                                                                                                                                 new XAttribute
                                                                                                                                     ("Include",
                                                                                                                                      "Microsoft.Net.Client.3.5"),
                                                                                                                                 new XElement
                                                                                                                                     ("Visible",
                                                                                                                                      "False"),
                                                                                                                                 new XElement
                                                                                                                                     ("ProductName",
                                                                                                                                      ".NET Framework 3.5 SP1 Client Profile"),
                                                                                                                                 new XElement
                                                                                                                                     ("Install",
                                                                                                                                      "false"))
                                                                                                                            ,
                                                                                                                            new XElement
                                                                                                                                ("BootstrapperPackage",
                                                                                                                                 new XAttribute
                                                                                                                                     ("Include",
                                                                                                                                      "Microsoft.Net.Framework.3.5.SP1"),
                                                                                                                                 new XElement
                                                                                                                                     ("Visible",
                                                                                                                                      "False"),
                                                                                                                                 new XElement
                                                                                                                                     ("ProductName",
                                                                                                                                      ".NET Framework 3.5 SP1"),
                                                                                                                                 new XElement
                                                                                                                                     ("Install",
                                                                                                                                      "true"))
                                                                                                                            ,
                                                                                                                            new XElement
                                                                                                                                ("BootstrapperPackage",
                                                                                                                                 new XAttribute
                                                                                                                                     ("Include",
                                                                                                                                      "Microsoft.Windows.Installer.3.1"),
                                                                                                                                 new XElement
                                                                                                                                     ("Visible",
                                                                                                                                      "False"),
                                                                                                                                 new XElement
                                                                                                                                     ("ProductName",
                                                                                                                                      "Windows Installer 3.1"),
                                                                                                                                 new XElement
                                                                                                                                     ("Install",
                                                                                                                                      "true")))
                                                                                                                   })
                                                                                  };

    }
}
