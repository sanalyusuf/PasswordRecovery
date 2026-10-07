namespace PasswordRecovery;

public sealed class MainForm : Form
{
    private const int HotkeyId = 1;

    private readonly Label _hwnd = new() { AutoSize = true };
    private readonly Label _className = new() { AutoSize = true };
    private readonly Label _status = new()
    {
        AutoSize = true,
        Text = "Bekleniyor. Deneme kutusunu aç, fareni şifre satırına getir.",
        Dock = DockStyle.Top,
        Padding = new Padding(0, 0, 0, 8),
    };
    private readonly TextBox _output = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 16f),
    };
    private readonly CheckBox _restore = new()
    {
        AutoSize = true,
        Text = "Yıldızı geri koy",
        Checked = true,
    };
    private readonly System.Windows.Forms.Timer _timer;
    private bool _hotkeyOk;

    public MainForm()
    {
        Text = "Password Recovery";
        Font = new Font("Segoe UI", 10f);
        BackColor = Color.FromArgb(244, 241, 234);
        ForeColor = Color.FromArgb(24, 26, 32);
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(680, 500);
        MinimumSize = new Size(520, 400);
        ShowInTaskbar = true;
        TopMost = true;

        var hint = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 8),
            Text = "Fareni bir kutunun üstüne getir.",
        };

        var save = new Button { Text = "Kaydet (Ctrl+O)", AutoSize = true, Height = 36 };
        save.Click += (_, _) => SaveResult();

        var practice = new Button { Text = "Deneme kutusu", AutoSize = true, Height = 36 };
        practice.Click += (_, _) => OpenPractice();

        var about = new Button { Text = "Nedir?", AutoSize = true, Height = 36 };
        about.Click += (_, _) => MessageBox.Show(this, "Bilgisayarınza kaydedilen şifreleri geri getirir.");

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
        };
        bar.Controls.Add(_restore);
        bar.Controls.Add(save);
        bar.Controls.Add(practice);
        bar.Controls.Add(about);

        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        host.Controls.Add(_output);
        host.Controls.Add(_status);
        host.Controls.Add(_className);
        host.Controls.Add(_hwnd);
        host.Controls.Add(hint);

        Controls.Add(host);
        Controls.Add(bar);

        _timer = new System.Windows.Forms.Timer { Interval = 100 };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _hotkeyOk = NativeMethods.RegisterHotKey(Handle, HotkeyId, NativeMethods.MOD_CONTROL, NativeMethods.VK_O);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
        if (_hotkeyOk)
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
        base.OnFormClosed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam == (IntPtr)HotkeyId)
            SaveResult();
        base.WndProc(ref m);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var probe = Reader.Look(Handle, _restore.Checked);
        _status.Text = probe.Status;
        if (!probe.UpdateText)
            return;

        _hwnd.Text = "";
        _className.Text = "";
        if (_output.Text != probe.Text)
            _output.Text = probe.Text;
    }

    private void SaveResult()
    {
        _timer.Stop();
        try
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Yakalanan yazıyı kaydet",
                Filter = "Metin (*.txt)|*.txt",
                FileName = "password.txt",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            File.WriteAllText(
                dialog.FileName,
                _output.Text + Environment.NewLine + _hwnd.Text + Environment.NewLine);
        }
        finally
        {
            _timer.Start();
        }
    }

    private void OpenPractice()
    {
        var box = new Form
        {
            Text = "Deneme kutusu",
            Size = new Size(420, 240),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true,
        };
        box.Controls.Add(new Label { Text = "Şifre", Location = new Point(16, 16), AutoSize = true });
        box.Controls.Add(new TextBox
        {
            Location = new Point(16, 40),
            Width = 360,
            Text = "deneme-sifre",
            PasswordChar = '●',
        });
        box.Controls.Add(new Label { Text = "Not", Location = new Point(16, 80), AutoSize = true });
        box.Controls.Add(new TextBox
        {
            Location = new Point(16, 104),
            Width = 360,
            Text = "bu yazı zaten görünür",
        });
        box.Show(this);
    }
}
