using System;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using RollbackToVS2008.Interfaces;

namespace RollbackToVS2008.Implementations
{
    public class TfsManager : IVersionControl, IDisposable
    {
        private readonly TfsTeamProjectCollection _tfs;
        private readonly VersionControlServer _versionControlServer;
        private readonly Workspace _workspace;
        
        public Uri Server { get; set; }
        public string Workspace { get; set; }

        public TfsManager(Uri server, string workspace)
        {
            Server = server;
            Workspace = workspace;
            _tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(Server, new UICredentialsProvider());
            _tfs.EnsureAuthenticated();
            _versionControlServer = _tfs.GetService<VersionControlServer>();
            _workspace = _versionControlServer.GetWorkspace(Workspace, _versionControlServer.AuthorizedUser);
        }

        public int CheckOutFile(string fileToCheckout)
        {
            return _workspace.PendEdit(fileToCheckout);
        }

        public int CheckInFile(string fileToCheckIn, string checkInComment)
        {
            var pendingChanges = _workspace.GetPendingChanges(fileToCheckIn);
            return _workspace.CheckIn(pendingChanges, checkInComment);
        }

        public int UndoCheckOutFile(string fileToUndoCheckout)
        {
            return _workspace.Undo(fileToUndoCheckout);
        }

        public int UndoCheckOutAll()
        {
            var pendingChanges = _workspace.GetPendingChanges();
            return _workspace.Undo(pendingChanges);
        }

        public int CheckInAll(string checkInComment)
        {
            var edits = _workspace.GetPendingChanges();
            return edits.Length > 0 ? _workspace.CheckIn(edits, checkInComment) : 0;
        }

        public void Dispose()
        {
            _tfs.Dispose();
        }
    }
}
