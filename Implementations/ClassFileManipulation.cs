using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RollbackToVS2008.Interfaces;

namespace RollbackToVS2008.Implementations
{
    class ClassFileManipulation : IFileManipulation
    {
        public IVersionControl VersionControl { get; set; }
        public string WorkspaceRoot { get; set; }

        public ClassFileManipulation(IVersionControl versionControl, string workspaceRoot)
        {
            VersionControl = versionControl;
            WorkspaceRoot = workspaceRoot;
        }

        public void ProcessFiles()
        {
            ProcessClasses();
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

        public void ProcessClasses()
        {
            var patternsToMatch = new Dictionary<string, string>
                                      {
                                          {
                                              "^Public Class",
                                              "\r\n<Serializable()> _\r\nPublic Class"
                                              },
                                          {
                                              "\r\nPublic Class",
                                              "\r\n<Serializable()> _\r\nPublic Class"
                                              },
                                          {
                                              "^public class",
                                              "\r\n[Serializable()]\r\npublic class"
                                              },
                                          {
                                              "\r\npublic class",
                                              "\r\n[Serializable()]\r\npublic class"
                                              }
                                      };
            // what files to open
            var fileTypesToOpen = new [] {"*.vb", "*.cs"};

            // get all the files we want and loop through them
            foreach (var fileTypeToOpen in fileTypesToOpen)
                foreach (var file in Directory.GetFiles(WorkspaceRoot
                                                        , fileTypeToOpen
                                                        , SearchOption.AllDirectories))
                {
                    // open
                    var originalContents = File.ReadAllText(file);
                    var contents = patternsToMatch.Aggregate(originalContents,
                                                             (current, match) =>
                                                             new Regex(match.Key).Replace(current, match.Value));

                    // find and replace

                    if (!originalContents.Equals(contents))
                    {
                        VersionControl.CheckOutFile(file);
                        File.WriteAllText(file, contents);
                    }
                }
        }
    }
}
