namespace RollbackToVS2008.Interfaces
{
    public interface IFileManipulation
    {
        IVersionControl VersionControl { get; set; }
        string WorkspaceRoot { get; set; }
        void ProcessFiles();
        int Commit(string checkInComment);
        int Rollback();
    }
}