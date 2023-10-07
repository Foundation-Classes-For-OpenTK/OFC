/*
 * Copyright 2019-2023 Robbyxp1 @ github.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */

using GLOFC.Utils;
using System;
using System.ComponentModel;
using System.Drawing;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Text box for numbers control
    /// </summary>
    /// <typeparam name="T">Type of number</typeparam>
    public abstract class GLNumberBox<T> : GLTextBox
    {
        /// <summary> Set the format to print the number in 
        /// See <href>https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings</href></summary>
        public string Format { get { return format; } set { format = value; base.Text = ConvertToString(Value); Check(); } }

        /// <summary> Set the format culture to use. Default is CurrentCulture </summary>
        public System.Globalization.CultureInfo FormatCulture { get { return culture; } set { culture = value; Check(); } }

        /// <summary> Set the number styles </summary>
        public System.Globalization.NumberStyles NumberStyles { get { return numstyles; } set { numstyles = value; Check(); } }

        /// <summary> Callback when value changes </summary>
        public Action<GLBaseControl> ValueChanged;
        /// <summary> Callback when validity of number changes. bool indicates if valid.</summary>
        public Action<GLBaseControl,bool> ValidityChanged;

        /// <summary> Minimum value to allow. If outside, InErrorCondition will be set </summary>
        public T Minimum { get { return minimum; } set { minimum = value; Check(); } }
        /// <summary> Maximum value to allow. If outside, InErrorCondition will be set </summary>
        public T Maximum { get { return maximum; } set { maximum = value; Check(); } }
        /// <summary> Is the current text in box valid? </summary>
        public bool IsValid { get { return ConvertFromString(base.Text, out T v); } }

        /// <summary> Compare this number box against another, and set the compare mode: -2 (less than -1(less) 0 (equal) 1 (greater) 2 (greater than) </summary>
        public void SetComparitor(GLNumberBox<T> other, int compare)        
        {
            othernumberbox = other;
            othercomparision = compare;
            Check();
        }

        /// <summary> Set the control blank but without an error </summary>
        public void SetBlank()          
        {
            base.Text = "";
            InErrorCondition = false;
        }

        /// <summary> Set the control to the last valid value (given by Value)</summary>
        public void SetNonBlank()       
        {
            base.Text = ConvertToString(Value);
            Check();
        }

        /// <summary> Get value of box (which is the last valid value) or set the value and the text box. No events are fired on set.</summary>
        public T Value                                          
        {
            get { return number; }
            set
            {
                number = value;
                base.Text = ConvertToString(number);
                Check();
            }
        }

        /// <summary> Get Text only. No set </summary>
        public new string Text { get { return base.Text; } set { System.Diagnostics.Debug.Assert(false, "Can't set Number box"); } }

        #region Implementation

        private void Check()
        {
            InErrorCondition = !IsValid;
        }

        private protected GLNumberBox(string  name, Rectangle pos ) : base(name,pos)
        {
        }

        private protected abstract string ConvertToString(T v);
        private protected abstract bool ConvertFromString(string t, out T number);
        private protected abstract bool AllowedChar(char c);

        private T number;
        private T minimum;
        private T maximum;
        private string format = "N";
        private System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
        private System.Globalization.NumberStyles numstyles = System.Globalization.NumberStyles.None;

        private protected GLNumberBox<T> othernumberbox { get; set; } = null;             // attach to another box for validation
        private protected int othercomparision { get; set; } = 0;              // aka -2 (<=) -1(<) 0 (=) 1 (>) 2 (>=)

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLMultiLineTextBox.TextChangedEvent()"/>
        protected override void TextChangedEvent()
        {
            if (ConvertFromString(Text, out T newvalue))
            {
                number = newvalue;

                OnValueChanged();

                if (InErrorCondition)
                    OnValidityChanged(true);

                InErrorCondition = false;
            }
            else
            {                               // Invalid, indicate
                if (!InErrorCondition)
                    OnValidityChanged(false);

                InErrorCondition = true;
            }

            base.TextChangedEvent();
        }

        /// <summary>
        /// Override this to see the value changed event. Default is to invoke ValueChanved
        /// </summary>
        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this);
        }

        /// <summary>
        /// Override this to see the validity changed event. Default is to invoke ValidityChanged
        /// </summary>
        /// <param name="valid"></param>

        protected virtual void OnValidityChanged(bool valid)
        {
            ValidityChanged?.Invoke(this,valid);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyPress(GLKeyEventArgs)"/>
        protected override void OnKeyPress(GLKeyEventArgs e) // limit keys to whats allowed for a double
        {
            if (AllowedChar(e.KeyChar))
            {
                base.OnKeyPress(e);
            }
            else
            {
                e.Handled = true;
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnFocusChanged(FocusEvent, GLBaseControl)"/>
        protected override void OnFocusChanged(FocusEvent evt, GLBaseControl fromto)
        {
            if ( evt == FocusEvent.Deactivated )     // if lost focus
            {
                if (!IsValid)           // if text box is not valid, go back to the original colour with no change event
                    Value = number;
            }
            base.OnFocusChanged(evt, fromto);
        }

        #endregion
    }

    /// <summary>1
    /// Number box for floats
    /// </summary>
    public class GLNumberBoxFloat : GLNumberBox<float>
    {
        /// <summary> Constructor with name, bounds and initial value </summary>
        public GLNumberBoxFloat(string name, Rectangle pos, float value) : base(name, pos)
        {
            NumberStyles = System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands;
            Minimum = float.MinValue;
            Maximum = float.MaxValue;
            Value = value;
            Format = "G";
        }

        /// <summary> Constructor with name, bounds, initial value, colours </summary>
        public GLNumberBoxFloat(string name, Rectangle pos, float value, Color backcolor, Color forecolor, bool enablethemer = true) : this(name, pos, value)
        {
            BackColor = backcolor;
            ForeColor = forecolor;
            EnableThemer = enablethemer;
        }

        /// <summary> Default conctructor </summary>
        public GLNumberBoxFloat() : this("NBF", DefaultWindowRectangle, 0)
        {
        }

        private protected override string ConvertToString(float v)
        {
            return v.ToString(Format, FormatCulture);
        }

        private protected override bool ConvertFromString(string t, out float number)
        {
            bool ok = float.TryParse(t, NumberStyles, FormatCulture, out number) &&
                      number >= Minimum && number <= Maximum;
            if (ok && othernumberbox != null)
                ok = number.CompareTo(othernumberbox.Value, othercomparision);
            return ok;
        }

        private protected override bool AllowedChar(char c)
        {
          return (char.IsDigit(c) || c == 8 ||
                    (c == FormatCulture.NumberFormat.CurrencyDecimalSeparator[0] && Text.IndexOf(FormatCulture.NumberFormat.CurrencyDecimalSeparator, StringComparison.Ordinal) == -1) ||
                    (c == FormatCulture.NumberFormat.NegativeSign[0] && SelectionStart == 0 && Minimum < 0)) ||
                    (c == FormatCulture.NumberFormat.NumberGroupSeparator[0] && (NumberStyles & System.Globalization.NumberStyles.AllowThousands) != 0);
        }
    }

    /// <summary>
    /// Number box for doubles
    /// </summary>
    public class GLNumberBoxDouble : GLNumberBox<double>
    {
        /// <summary> Constructor with name, bounds and initial value </summary>
        public GLNumberBoxDouble(string name, Rectangle pos, double value) : base(name, pos)
        {
            NumberStyles = System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands;
            Minimum = double.MinValue;
            Maximum = double.MaxValue;
            Value = value;
            Format = "G";
        }

        /// <summary> Constructor with name, bounds, initial value, colours </summary>
        public GLNumberBoxDouble(string name, Rectangle pos, double value, Color backcolor, Color forecolor, bool enablethemer = true) : this(name, pos, value)
        {
            BackColor = backcolor;
            ForeColor = forecolor;
            EnableThemer = enablethemer;
        }

        /// <summary> Default conctructor </summary>
        public GLNumberBoxDouble() : this("NBD", DefaultWindowRectangle, 0)
        {
        }

        private protected override string ConvertToString(double v)
        {
            return v.ToString(Format, FormatCulture);
        }
        private protected override bool ConvertFromString(string t, out double number)
        {
            bool ok = double.TryParse(t, NumberStyles, FormatCulture, out number) &&
                number >= Minimum && number <= Maximum;
            if (ok && othernumberbox != null)
                ok = number.CompareTo(othernumberbox.Value, othercomparision);
            return ok;
        }

        private protected override bool AllowedChar(char c)
        {
            return (char.IsDigit(c) || c == 8 ||
                (c == FormatCulture.NumberFormat.CurrencyDecimalSeparator[0] && Text.IndexOf(FormatCulture.NumberFormat.CurrencyDecimalSeparator) == -1) ||
                (c == FormatCulture.NumberFormat.NegativeSign[0] && SelectionStart == 0 && Minimum < 0)) ||
                (c == FormatCulture.NumberFormat.NumberGroupSeparator[0] && (NumberStyles & System.Globalization.NumberStyles.AllowThousands) != 0);
        }
    }

    /// <summary>
    /// Number box for long
    /// </summary>
    public class GLNumberBoxLong : GLNumberBox<long>
    {
        /// <summary> Constructor with name, bounds and initial value </summary>
        public GLNumberBoxLong(string name, Rectangle pos, long value) : base(name, pos)
        {
            NumberStyles = System.Globalization.NumberStyles.AllowThousands;
            Minimum = long.MinValue;
            Maximum = long.MaxValue;
            Value = value;
            Format = "D";
        }

        /// <summary> Constructor with name, bounds, initial value, colours </summary>
        public GLNumberBoxLong(string name, Rectangle pos, long value, Color backcolor, Color forecolor, bool enablethemer = true) : this(name, pos, value)
        {
            BackColor = backcolor;
            ForeColor = forecolor;
            EnableThemer = enablethemer;
        }

        /// <summary> Default conctructor </summary>
        public GLNumberBoxLong() : this("NBL", DefaultWindowRectangle, 0)
        {
        }

        private protected override string ConvertToString(long v)
        {
            return v.ToString(Format, FormatCulture);
        }
        
        private protected override bool ConvertFromString(string t, out long number)
        {
            bool ok = long.TryParse(t, NumberStyles, FormatCulture, out number) &&
                            number >= Minimum && number <= Maximum;
            if (ok && othernumberbox != null)
                ok = number.CompareTo(othernumberbox.Value, othercomparision);
            return ok;
        }

        private protected override bool AllowedChar(char c)
        {
            return (char.IsDigit(c) || c == 8 ||
                (c == FormatCulture.NumberFormat.NegativeSign[0] && SelectionStart == 0 && Minimum < 0) ||
                (c == FormatCulture.NumberFormat.NumberGroupSeparator[0]));
        }
    }


}
