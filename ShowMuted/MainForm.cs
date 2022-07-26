using ShowMuted.KeyboardHook;
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
            if (Properties.Settings.Default.FormSize.Width != 0 && Properties.Settings.Default.FormSize.Height != 0)
            {
                WindowState = Properties.Settings.Default.FormState;

                if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;

                Location = Properties.Settings.Default.FormLocation;
                Size = Properties.Settings.Default.FormSize;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.FormState = WindowState;
            if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.FormLocation = Location;
                Properties.Settings.Default.FormSize = Size;
            }
            else
            {
                // Save the RestoreBounds if the form is minimized or maximized
                Properties.Settings.Default.FormLocation = RestoreBounds.Location;
                Properties.Settings.Default.FormSize = RestoreBounds.Size;
            }

            Properties.Settings.Default.Save();

            _keyboardHookManager.StopHook();
        }
    }
}
