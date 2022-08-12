using System;
using System.Collections.Generic;

namespace ShowMuted.KeyboardHook
{
    internal class Keybind : IEquatable<Keybind>
    {
        public readonly int VirtualKeyCode;
        public readonly List<ModifierKeys> Modifiers;

        public Keybind(IEnumerable<ModifierKeys> modifiers, int vkCode)
        {
            VirtualKeyCode = vkCode;
            Modifiers = new List<ModifierKeys>(modifiers);
        }

        public bool Equals(Keybind other)
        {
            if (other == null)
                return false;

            if (VirtualKeyCode != other.VirtualKeyCode)
                return false;

            if (Modifiers.Count != other.Modifiers.Count)
                return false;

            foreach (ModifierKeys modifier in Modifiers)
            {
                if (!other.Modifiers.Contains(modifier))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Contains(Keybind other)
        {
            if (other == null)
                return false;

            if (VirtualKeyCode != other.VirtualKeyCode)
                return false;

            if (Modifiers.Count < other.Modifiers.Count)
                return false;

            foreach (ModifierKeys modifier in other.Modifiers)
            {
                if (!Modifiers.Contains(modifier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
