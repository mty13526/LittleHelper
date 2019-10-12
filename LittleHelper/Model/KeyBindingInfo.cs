using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LittleHelper.Model
{
    public class KeyBindingInfo
    {

        public class Keys
        {
            public ModifierKeys[] ModifierKeys;
            public Key NormalKey;

            public override string ToString()
            {
                string ret = String.Join(" + ", this.ModifierKeys);

                if (ret != String.Empty && NormalKey != Key.None)
                {
                    ret += " + ";
                }

                if (NormalKey != Key.None)
                {
                    ret += this.NormalKey.ToString();
                }

                return ret;
            }
        };

        public Keys BindedKeys { get; private set; }
        public Keys UnbindedKeys { get; private set; }

        public KeyBindingInfo()
        {

        }


        public ModifierKeys Modifiers
        {
            get
            {
                if (this.UnbindedKeys == null) return ModifierKeys.None;

                ModifierKeys ret = ModifierKeys.None;
                foreach (var i in this.UnbindedKeys.ModifierKeys)
                {
                    ret |= i;
                }

                return ret;
            }
            set
            {
                List<ModifierKeys> result = new List<ModifierKeys>();

                foreach (ModifierKeys i in Enum.GetValues(typeof(ModifierKeys)))
                {
                    if (i == ModifierKeys.None) continue;

                    if ((value & i) == i)
                    {
                        result.Add(i);
                    }
                }

                if (this.UnbindedKeys == null) this.UnbindedKeys = new Keys();
                this.UnbindedKeys.ModifierKeys = result.ToArray();
            }
        }

        public override string ToString()
        {
            if (this.UnbindedKeys == null)
            {
                return this.BindedKeys.ToString();
            }
            else
            {
                return this.UnbindedKeys.ToString();
            }
        }

        public void Apply()
        {
            this.BindedKeys = this.UnbindedKeys;
            this.UnbindedKeys = null;
        }
    }
}
