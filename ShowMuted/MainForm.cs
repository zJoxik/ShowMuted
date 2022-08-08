using ShowMuted.KeyboardHook;
using ShowMuted.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShowMuted
{
    public partial class MainForm : Form
    {
        private readonly Color _mutedColor = Color.Red;
        private readonly Color _unmutedColor = Color.Lime;
        private bool _isMuted;

        private readonly KeyboardHookManager _keyboardHookManager;

        public MainForm()
        {
            InitializeComponent();

            _keyboardHookManager = new KeyboardHookManager();
            _keyboardHookManager.StartHook();

            // https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
            _keyboardHookManager.RegisterHotkey(0x91, () =>
            {
                SetColor();
            });
        }

        private void SetColor()
        {
            _isMuted = !_isMuted;
            BackColor = _isMuted ? _mutedColor : _unmutedColor;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (Settings.Default.FormSize.Width != 0 && Settings.Default.FormSize.Height != 0)
            {
                WindowState = Settings.Default.FormState;

                if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;

                Location = Settings.Default.FormLocation;
                Size = Settings.Default.FormSize;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.FormState = WindowState;
            if (WindowState == FormWindowState.Normal)
            {
                Settings.Default.FormLocation = Location;
                Settings.Default.FormSize = Size;
            }
            else
            {
                Settings.Default.FormLocation = RestoreBounds.Location;
                Settings.Default.FormSize = RestoreBounds.Size;
            }

            Settings.Default.Save();

            _keyboardHookManager.StopHook();
        }
    }
}
