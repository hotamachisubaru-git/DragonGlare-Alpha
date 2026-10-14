global using System;
global using System.Collections.Generic;
global using System.Drawing;
global using System.IO;
global using System.Linq;
global using DragonGlare.UnityPort;

using System.Security.Cryptography;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace DragonGlare.UnityPort
{
    internal static class CryptoCompatibility
    {
        internal static byte[] Sha256(byte[] payload)
        {
            using var algorithm = SHA256.Create();
            return algorithm.ComputeHash(payload);
        }

        internal static byte[] HmacSha256(byte[] key, byte[] payload)
        {
            using var algorithm = new HMACSHA256(key);
            return algorithm.ComputeHash(payload);
        }
    }

    internal static class SaveFileOperations
    {
        internal static void Commit(string temporaryPath, string destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                File.Replace(temporaryPath, destinationPath, null);
            }
            else
            {
                File.Move(temporaryPath, destinationPath);
            }
        }
    }
}
