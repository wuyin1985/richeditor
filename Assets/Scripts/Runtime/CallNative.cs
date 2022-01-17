using System.Runtime.InteropServices;

namespace PathEditor
{
    public static class CallNative
    {
        [DllImport("hashtoollib.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ulong c_get_hash(byte[] utf8Text);
        
        [DllImport("hashtoollib.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern void c_save_hash(byte[] utf8FilePath);
    }
}