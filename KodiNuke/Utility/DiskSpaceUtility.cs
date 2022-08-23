using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KodiNuke.Utility
{
    public static class DiskSpaceUtility
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);

        public static long GetFreeDiskSpaceBytes(string path)
        {
            if (GetDiskFreeSpaceEx(path, out var freeSpace, out _, out _))
                return (long)freeSpace;

            return 0;
        }
    }
}
