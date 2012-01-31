using System;

namespace RollbackToVS2008.Interfaces
{
    public interface IVersionControl
    {
        Uri Server { get; set; }
        string Workspace { get; set; }
        int CheckOutFile(string fileToCheckout);
        int CheckInFile(string fileToCheckIn, string checkInComment);
        int CheckInAll(string checkInComment);
        int UndoCheckOutFile(string fileToUndoCheckout);
        int UndoCheckOutAll();
    }
}
