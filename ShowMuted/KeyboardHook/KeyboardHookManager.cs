using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ShowMuted.KeyboardHook
{
    internal class KeyboardHookManager
    {
        private Keybind _registeredKey;
        private Action _registeredAction;

        private readonly HashSet<ModifierKeys> _pressedModifierKeys;

        private readonly object _modifiersLock = new object();
        private LowLevelKeyboardProc _hook;
        private bool _isActive;

        public KeyboardHookManager() => _pressedModifierKeys = new HashSet<ModifierKeys>();

        #region Public Methods
        public void StartHook()
        {
            if (_isActive) return;

            _hook = HookCallback;
            _hookID = SetHook(_hook);
            _isActive = true;
        }

        public void StopHook()
        {
            if (!_isActive) return;

            if (!UnhookWindowsHookEx(_hookID)) throw new CouldNotUnhookException();
            _isActive = false;
        }

        public void RegisterHotkey(int virtualKeyCode, Action action) => RegisterHotkey(new ModifierKeys[0], virtualKeyCode, action);

        public void RegisterHotkey(ModifierKeys modifiers, int virtualKeyCode, Action action)
        {
            ModifierKeys[] allModifiers = Enum.GetValues(typeof(ModifierKeys)).Cast<ModifierKeys>().ToArray();

            ModifierKeys[] selectedModifiers = allModifiers.Where(modifier => modifiers.HasFlag(modifier)).ToArray();

            RegisterHotkey(selectedModifiers, virtualKeyCode, action);
        }

        public void RegisterHotkey(ModifierKeys[] modifiers, int virtualKeyCode, Action action)
        {
            var keybind = new Keybind(modifiers, virtualKeyCode);
            if (keybind.Equals(_registeredKey)) throw new KeyAlreadyRegisteredException();

            _registeredKey = keybind;
            _registeredAction = action;
        }
        #endregion

        #region Private Methods
        private void HandleKeyPress(int vkCode)
        {
            var pressedKey = new Keybind(_pressedModifierKeys, vkCode);

            if (!pressedKey.Contains(_registeredKey)) return;

            _registeredAction?.Invoke();
        }
        #endregion

        #region Keyboard Hook
        // Based on https://docs.microsoft.com/en-us/archive/blogs/toub/low-level-keyboard-hook-in-c
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            IntPtr user32Lib = LoadLibrary("user32");

            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, user32Lib, 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                ThreadPool.QueueUserWorkItem(HandleSingleKeyboardInput, new KeyboardParams(wParam, vkCode));
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void HandleSingleKeyboardInput(object keyboardParamsObj)
        {
            var keyboardParams = (KeyboardParams)keyboardParamsObj;
            IntPtr wParam = keyboardParams.WParam;
            int vkCode = keyboardParams.VkCode;

            ModifierKeys? modifierKey = ModifierKeysUtility.GetModifierKeyFromVKCode(vkCode);

            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                if (modifierKey != null)
                {
                    lock (_modifiersLock)
                    {
                        _pressedModifierKeys.Add(modifierKey.Value);
                    }
                }
            }

            if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                if (modifierKey != null)
                {
                    lock (_modifiersLock)
                    {
                        _pressedModifierKeys.Remove(modifierKey.Value);
                    }
                }

                HandleKeyPress(vkCode);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hmod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpLibFileName);
        #endregion
    }

    internal struct KeyboardParams
    {
        public IntPtr WParam;
        public int VkCode;

        public KeyboardParams(IntPtr wParam, int vkCode)
        {
            WParam = wParam;
            VkCode = vkCode;
        }
    }

    #region Exceptions
    public class KeyboardHookException : Exception
    {
    }

    public class KeyAlreadyRegisteredException : KeyboardHookException
    {
    }

    public class CouldNotUnhookException : KeyboardHookException
    {
    }
    #endregion
}
