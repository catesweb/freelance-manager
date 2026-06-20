using System;
using System.Security.Cryptography;
using System.Text;

namespace FreelanceManager.App.Services;

// ponytail: DPAPI is Windows-only and so is this app (Velopack win-x64). Scope the warning here
// rather than retargeting the TFM to net10.0-windows.
#pragma warning disable CA1416

/// <summary>
/// Per-user, encrypted-at-rest secret storage via Windows DPAPI. Used for the SMTP password
/// so the SQLite file never holds a readable credential.
/// ponytail: Windows-only (CurrentUser scope). Fine — this is a Windows desktop app.
/// </summary>
public static class Dpapi
{
    public static string? Encrypt(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return null;
        var bytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plaintext), null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    public static string? Decrypt(string? cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return null;
        try
        {
            var bytes = ProtectedData.Unprotect(
                Convert.FromBase64String(cipher), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException) { return null; }   // wrong user / corrupted blob
    }
}
