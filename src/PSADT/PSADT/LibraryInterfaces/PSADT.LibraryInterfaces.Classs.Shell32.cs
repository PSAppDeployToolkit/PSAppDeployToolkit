using PSADT.PInvokes;

namespace PSADT.LibraryInterfaces
{
    public static class Shell32
    {
        public static int SetCurrentProcessExplicitAppUserModelID(string AppID)
        {
            return NativeMethods.SetCurrentProcessExplicitAppUserModelID(AppID);
        }
    }
}
