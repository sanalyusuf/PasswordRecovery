using System.Text;

namespace PasswordRecovery;

internal static class Reader
{
    public readonly record struct Probe(
        bool UpdateText,
        IntPtr Hwnd,
        string ClassName,
        string Text,
        string Status)
    {
        public static Probe Keep(string status) => new(false, IntPtr.Zero, "", "", status);
    }

    public static Probe Look(IntPtr ourWindow, bool restoreMask)
    {
        if (!NativeMethods.GetCursorPos(out var point))
            return Probe.Keep("Fare konumu okunamadı.");

        var hwnd = NativeMethods.WindowFromPoint(point);
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
            return Probe.Keep("İmlecin altında pencere yok.");

        if (NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT) == ourWindow)
            return Probe.Keep("");

        var className = ClassNameOf(hwnd);
        var style = NativeMethods.GetWindowStyle(hwnd).ToInt64();
        var passwordStyle = (style & NativeMethods.ES_PASSWORD) != 0;
        var isEdit = className.Contains("EDIT", StringComparison.OrdinalIgnoreCase) || passwordStyle;

        var passwordChar = '\0';
        if (isEdit)
        {
            var raw = NativeMethods.SendMessage(
                hwnd,
                NativeMethods.EM_GETPASSWORDCHAR,
                IntPtr.Zero,
                IntPtr.Zero);
            passwordChar = (char)(raw.ToInt64() & 0xFFFF);
        }

        var masked = isEdit && (passwordStyle || passwordChar != '\0');
        string text;

        if (masked)
        {
            NativeMethods.SendMessage(hwnd, NativeMethods.EM_SETPASSWORDCHAR, IntPtr.Zero, IntPtr.Zero);
            text = ReadText(hwnd);

            if (restoreMask)
            {
                var restore = passwordChar == '\0' ? (IntPtr)'*' : (IntPtr)passwordChar;
                NativeMethods.SendMessage(hwnd, NativeMethods.EM_SETPASSWORDCHAR, restore, IntPtr.Zero);
            }
        }
        else
        {
            text = ReadText(hwnd);
        }

        var status = !isEdit
            ? "Şifre kutusu değil. "
            : masked
                ? "Şifreli kutu."
                : "Normal kutu. Gizlenmiş bir şey yok.";

        return new Probe(true, hwnd, className, text, status);
    }

    private static string ClassNameOf(IntPtr hwnd)
    {
        var buffer = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private static string ReadText(IntPtr hwnd)
    {
        var length = (int)NativeMethods.SendMessage(
            hwnd,
            NativeMethods.WM_GETTEXTLENGTH,
            IntPtr.Zero,
            IntPtr.Zero);

        if (length <= 0)
            return "";

        if (length > 8192)
            length = 8192;

        var buffer = new StringBuilder(length + 1);
        NativeMethods.SendMessage(hwnd, NativeMethods.WM_GETTEXT, (IntPtr)(length + 1), buffer);
        return buffer.ToString();
    }
}
