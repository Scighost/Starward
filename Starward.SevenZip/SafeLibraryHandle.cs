using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;

namespace Starward.SevenZip
{
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeLibraryHandle() : base(true)
        {
        }

        /// <summary>Release library handle</summary>
        /// <returns>true if the handle was released</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected override bool ReleaseHandle()
        {
            return Kernel32Dll.FreeLibrary(handle);
        }
    }
}