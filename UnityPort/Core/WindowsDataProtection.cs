using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DragonGlare.UnityPort;

/// <summary>Windows CurrentUser DPAPI compatible with the original MonoGame saves.</summary>
internal static class WindowsDataProtection
{
    private const uint CryptProtectUiForbidden = 1;

    internal static byte[] Protect(byte[] payload, byte[] entropy, uint flags)
        => Transform(payload, entropy, flags, encrypt: true);

    internal static byte[] Unprotect(byte[] payload, byte[] entropy, uint flags)
        => Transform(payload, entropy, flags, encrypt: false);

    private static byte[] Transform(byte[] payload, byte[] entropy, uint flags, bool encrypt)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("DragonGlare save encryption requires Windows DPAPI.");
        }

        var input = Allocate(payload);
        var additionalEntropy = Allocate(entropy);
        DataBlob output = default;
        try
        {
            var succeeded = encrypt
                ? CryptProtectData(ref input, null, ref additionalEntropy, IntPtr.Zero, IntPtr.Zero,
                    flags | CryptProtectUiForbidden, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, ref additionalEntropy, IntPtr.Zero, IntPtr.Zero,
                    flags | CryptProtectUiForbidden, out output);
            if (!succeeded)
            {
                throw new CryptographicException(new Win32Exception(Marshal.GetLastWin32Error()).Message);
            }

            var result = new byte[output.Length];
            if (output.Length > 0)
            {
                Marshal.Copy(output.Data, result, 0, result.Length);
            }

            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(input.Data);
            Marshal.FreeHGlobal(additionalEntropy.Data);
            if (output.Data != IntPtr.Zero)
            {
                LocalFree(output.Data);
            }
        }
    }

    private static DataBlob Allocate(byte[] bytes)
    {
        var pointer = Marshal.AllocHGlobal(Math.Max(1, bytes.Length));
        if (bytes.Length > 0)
        {
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
        }

        return new DataBlob { Length = bytes.Length, Data = pointer };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Length;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref DataBlob input, string? description,
        ref DataBlob entropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description,
        ref DataBlob entropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
