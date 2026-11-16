using CefKit.Interop;

namespace CefKit.Native;

/// <summary>
/// Conversions between .NET strings and CEF UTF-16 strings (cef_string_t).
/// </summary>
internal static unsafe class CefStrings
{
    /// <summary>
    /// Reads a string that CEF passed in; a null pointer reads as null.
    /// </summary>
    public static string? Read(cef_string_utf16_t* value) =>
        value == null ? null
        : value->str == null || value->length == 0 ? string.Empty
        : new string(value->str, 0, checked((int)value->length));

    /// <summary>
    /// Reads a string returned by a CEF function (cef_string_userfree_t) and frees it.
    /// </summary>
    public static string? ReadUserFree(cef_string_utf16_t* value)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            return Read(value);
        }
        finally
        {
            Methods.cef_string_userfree_utf16_free(value);
        }
    }

    /// <summary>
    /// Copies a .NET string into a CEF string field owned by the caller; free it with <see cref="Clear"/>.
    /// </summary>
    public static void Set(cef_string_utf16_t* target, string? value)
    {
        Methods.cef_string_utf16_clear(target);
        if (!string.IsNullOrEmpty(value))
        {
            fixed (char* chars = value)
            {
                Methods.cef_string_utf16_set(chars, (nuint)value.Length, target, 1);
            }
        }
    }

    /// <summary>
    /// Frees a CEF string set by <see cref="Set"/>.
    /// </summary>
    public static void Clear(cef_string_utf16_t* target) => Methods.cef_string_utf16_clear(target);

    /// <summary>
    /// A CEF string over the pinned characters of a .NET string for a const cef_string_t* argument: CEF neither
    /// frees nor keeps it (it copies what it needs).
    /// </summary>
    public static cef_string_utf16_t View(char* chars, string? value) =>
        new() { str = chars, length = (nuint)(value?.Length ?? 0) };
}
