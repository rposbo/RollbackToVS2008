using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using RollbackToVS2008.Implementations;
using RollbackToVS2008.Interfaces;

namespace RollbackToVS2008
{
    class Program
    {
        private static void Main()
        {
            // setup
            const string directoryToTraverse = @"D:\My\Project\";
            var versionControlServer = new Uri("http:/my-tfsserver:8080");
            const string workspaceName = "My Workspace Name";
            const string checkinComment = "checking in a change about a thing";

            var container = new WindsorContainer()
                .Register(
                    Component.For<IVersionControl>().ImplementedBy<TfsManager>()
                        .DependsOn(new {server = versionControlServer})
                        .DependsOn(new {workspace = workspaceName})
                );

            container.Register(
                Component.For<IFileManipulation>().ImplementedBy<VsFileManipulation>()
                    .DependsOn(new {versionControl = container.Resolve<IVersionControl>()})
                    .DependsOn(new {workspaceRoot = directoryToTraverse})
                    .DependsOn(new {toggleDirection = VsFileManipulation.Direction.From2008To2010 }));

            var fileManager = container.Resolve<IFileManipulation>();

            fileManager.ProcessFiles();
            var changeSet = 0;// fileManager.Commit(checkinComment);

            Console.WriteLine(Convert.ToBoolean(changeSet)
                                  ? String.Format("Completed. Checked in as changeset: {0}", changeSet)
                                  : "Commit process didn't create a changeset number");

            Console.ReadKey();
        }       
    }
}