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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// A single line text box control with autocomplete
    /// Set InErrorCondition if required, next text entry will clear text box.
    /// </summary>
    public class GLTextBoxAutoComplete : GLTextBox
    {
        /// <summary> Callback to collect a list, into the sorted set, of autocomplete texts guided by string
        /// Fired in a thread, first before UI perform</summary>
        public Action<string, GLTextBoxAutoComplete, SortedSet<string>> PerformAutoCompleteInThread { get; set; } = null;
        /// <summary> Callback to collect a list, into the sorted set, of autocomplete texts guided by string
        /// Fired in the UI thread, after the thread perform</summary>
        public Action<string, GLTextBoxAutoComplete, SortedSet<string>> PerformAutoCompleteInUIThread { get; set; } = null;

        /// <summary> Callback. Run when use hits return or clicks on an autocompleted item in drop down, runs in the UI thread</summary>
        public Action<GLTextBoxAutoComplete> SelectedEntry { get; set; } = null;        

        /// <summary> Delay in ms before autocomplete starts</summary>
        public int AutoCompleteInitialDelay { get; set; } = 500;

        /// <summary> Show wait button </summary>
        public bool ShowEndButton { get { return endbuttonon; } set { endbuttonon = value; EndButton.Visible = endbuttonon; InvalidateLayout(); } }

        /// <summary> List box for autocomplete. For Themeing</summary>
        public GLListBox ListBox { get; set; }

        /// <summary> Wait button. For Themeing. Back/Face is set to transparent normally so only icon shows</summary>
        public GLButton EndButton { get; set; }

        /// <summary> Drop down shown</summary>
        public bool DropDownShown { get; private set; }

        /// <summary> Constructor with name, bounds and optional text </summary>
        public GLTextBoxAutoComplete(string name, Rectangle pos, string text = "", bool enablethemer = true) : base(name, pos, text, enablethemer)
        {
            initialdelaytimer.Tick += InitialDelayOver;
            autocompleteinuitimer.Tick += AutoCompleteInUI;             // this timer is just used to trigger a UI call via a Polledtimer
            autocompletefinishedtimer.Tick += AutoCompleteFinished;     // this timer is just used to trigger the finished call via a Polledtimer

            ListBox = new GLListBox();
            ListBox.AutoSize = true;
            ListBox.SelectedIndexChanged += (c0, i) => {
                //System.Diagnostics.Debug.WriteLine($"List box selected {ListBox.SelectedItem}");
                CancelAutoComplete();
                Text = ListBox.SelectedItem;
                OnTextChanged();            // cause the text changed event..
                SelectedEntry?.Invoke(this);
            };
            ListBox.OtherKeyPressed += (c1, e) => { if (e.KeyCode == System.Windows.Forms.Keys.Delete) CancelAutoComplete(); };
            ListBox.Name = name + "_LB";
            ListBox.TopMost = true;
            ListBox.ShowFocusHighlight = true;
            ListBox.EnableThemer = false;

            EndButton = new GLButton(name + "_WB", new Rectangle(0, 0, 24, 24), GLOFC.Properties.Resources.ArrowDown, true);        // stretch
            EndButton.Visible = false;
            EndButton.BackColor = EndButton.ButtonFaceColor = Color.Transparent;
            EndButton.BorderWidth = 0;
            EndButton.EnableThemer = false;
            EndButton.Dock = DockingType.RightCenter;
            EndButton.RejectFocus = true;
            EndButton.Click += (s, e) =>
            {
                if (DropDownShown)
                    CancelAutoComplete();
                else
                {
                    Text = "";
                    AutoComplete("");
                }
            };

            this.Add(EndButton);
        }

        /// <summary> Constructor with name, bounds, text, colors</summary>
        public GLTextBoxAutoComplete(string name, Rectangle pos, string text, Color backcolor, Color forecolor, bool enablethemer = true) : this(name, pos, text, enablethemer)
        {
            BackColor = backcolor;
            ForeColor = forecolor;
        }

        /// <summary> Default constructor </summary>
        public GLTextBoxAutoComplete() : this("TBAC?", DefaultWindowRectangle, "")
        {
        }

        /// <summary> Ask for autocomplete on this string </summary>
        public void AutoComplete(string autocomplete)           // call to autocomplete this
        {
            autocompletestring = autocomplete;
            InitialDelayOver(null, 0);
        }

        /// <summary> Cancel the autocomplete.  </summary>
        public void CancelAutoComplete()       
        {
            //System.Diagnostics.Debug.WriteLine("{0} AC cancelled", Environment.TickCount % 10000);
            // Sometimes, the user is quicker than the timer, and has commited to a selection before the results even come back.
            initialdelaytimer.Stop();
            autocompletefinishedtimer.Stop();
            Detach(ListBox);    // detach don't free whole list box
            DropDownShown = false;

            this.SetFocus();

            EndButton.Image = GLOFC.Properties.Resources.ArrowDown;

            if ( executingautocomplete )
            {
                //System.Diagnostics.Debug.WriteLine(".. executing AC, tell it to ignore result");
                restartautocomplete = false;
                ignoreautocomplete = true;
            }
            else
            {
                restartautocomplete = false;
                ignoreautocomplete = false;
            }
        }


        #region Implementation

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.PerformRecursiveLayout()"/>
        protected override void PerformRecursiveLayout()
        {
            ExtraPadding = new PaddingType(0, 0, endbuttonon ? EndButton.Width : 0, 0);
            base.PerformRecursiveLayout();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLMultiLineTextBox.TextChangedEvent()"/>
        // override this instead of OnTextChanged event so we don't get double entry when list box changes
        protected override void TextChangedEvent()
        {
            //System.Diagnostics.Debug.WriteLine("{0} AC text change event", Environment.TickCount % 10000);

            if (InErrorCondition)               // cancel any error condition on typing
                InErrorCondition = false;

            if (PerformAutoCompleteInThread != null || PerformAutoCompleteInUIThread != null )
            {
                if (!executingautocomplete)     // if not executing, start a timeout
                {
                    //System.Diagnostics.Debug.WriteLine("{0} AC Start timer", Environment.TickCount % 10000);
                    initialdelaytimer.Start(AutoCompleteInitialDelay);
                    autocompletestring = String.Copy(this.Text);    // a copy in case the text box changes it after complete starts
                }
                else
                {                               // we are executing a long one, but another autocomplete needs to be done
                    //System.Diagnostics.Debug.WriteLine("{0} AC running, restart search", Environment.TickCount % 10000);
                    autocompletestring = String.Copy(this.Text);
                    restartautocomplete = true;
                }
            }

            base.OnTextChanged();   // this calls the TextChanged event handler
        }

        /// <summary>
        /// Override on focus changed to detect changes in focus away from us
        /// </summary>
        /// <param name="evt">Focus event</param>
        /// <param name="fromto">from/to control</param>
        protected override void OnFocusChanged(FocusEvent evt, GLBaseControl fromto)
        {
            base.OnFocusChanged(evt, fromto);

            if (evt == FocusEvent.Deactivated && fromto != ListBox)
            {
                //System.Diagnostics.Debug.WriteLine("{0} AC focus {1} not to list box so cancel", Environment.TickCount % 10000, evt);
                CancelAutoComplete();
            }
        }

        // triggered by initialdelaytimer, do an autocomplete
        private void InitialDelayOver(PolledTimer t, long tick)
        {
            EndButton.Image = GLOFC.Properties.Resources.Wait;
            executingautocomplete = true;
            ThreadAutoComplete = new System.Threading.Thread(new System.Threading.ThreadStart(AutoCompleteThread));
            ThreadAutoComplete.Name = "AutoComplete";
            ThreadAutoComplete.Start();
        }

        private void AutoCompleteThread()     // in a thread..
        {
            do
            {
                //System.Diagnostics.Debug.WriteLine("{0} Begin AC", Environment.TickCount % 10000);
                restartautocomplete = false;

                autocompletestrings = new SortedSet<string>();

                if ( PerformAutoCompleteInThread != null)           // first see if a thread wants action
                {
                    //System.Diagnostics.Debug.WriteLine("AC in thread");
                    PerformAutoCompleteInThread(string.Copy(autocompletestring), this, autocompletestrings);
                }

                if ( restartautocomplete == false &&  PerformAutoCompleteInUIThread != null )        // then see if the UI wants action, don't do this is restart auto complete is set
                {
                    //System.Diagnostics.Debug.WriteLine("AC Fire in ui");
                    autocompleteinuitimer.FireNow();                // fire a UI thread timer off, which in polled, will call AutoCompleteInUI function
                    AutocompleteUIDone.WaitOne();                   // and stop thread until AutoCompleteInUI done
                    //System.Diagnostics.Debug.WriteLine("AC Fire in ui done");
                }

                //System.Diagnostics.Debug.WriteLine("{0} AC finish func ret {1} restart {2}", Environment.TickCount % 10000, autocompletestrings?.Count, restartautocomplete);
            } while (restartautocomplete == true);

            autocompletefinishedtimer.FireNow();                    // fire it immediately.  Next timer call around will trigger in correct thread.  This is thread safe.
        }

        private void AutoCompleteInUI(PolledTimer t, long tick)      // in UI thread, fired by autocompleteinui timer
        {
            //System.Diagnostics.Debug.WriteLine("{0} AC Perform in UI ", tick);
            PerformAutoCompleteInUIThread.Invoke(string.Copy(autocompletestring), this, autocompletestrings);        // we know its not null
            AutocompleteUIDone.Set();
        }
            
        private void AutoCompleteFinished(PolledTimer t, long tick)        // in UI thread
        {
            executingautocomplete = false;

            //System.Diagnostics.Debug.WriteLine($"{tick} AC finished results {autocompletestrings?.Count} ignore {ignoreautocomplete}");

            if (autocompletestrings != null && autocompletestrings.Count > 0 && !ignoreautocomplete)
            {
                //foreach (var s in autocompletestrings) { System.Diagnostics.Debug.WriteLine("Autocomplete String " + s); }

                Point sc = FindScreenCoords(new Point(ClientLeftMargin, Height + 1));
                ListBox.Font = Font;
                ListBox.Location = sc;
                ListBox.Items = autocompletestrings.ToList();
                ListBox.ScaleWindow = FindScaler();
                ListBox.Owner = this;
                ListBox.Name = Name + "_LB";

                if (ListBox.Parent == null)
                    AddToDesktop(ListBox);

                EndButton.Image = GLOFC.Properties.Resources.ArrowUp;
                DropDownShown = true;
            }
            else
            {
                EndButton.Image = GLOFC.Properties.Resources.ArrowDown;
                Detach(ListBox);
                DropDownShown = false;
            }

            ignoreautocomplete = false;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyDown(GLKeyEventArgs)"/>
        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                if (ListBox.Visible)
                {
                    if (e.KeyCode == System.Windows.Forms.Keys.Up)
                    {
                        ListBox.FocusUp();
                    }
                    else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                    {
                        ListBox.FocusDown();
                    }
                    else if (e.KeyCode == System.Windows.Forms.Keys.PageUp)
                    {
                        ListBox.FocusUp(ListBox?.DisplayableItems ?? 0);
                    }
                    else if (e.KeyCode == System.Windows.Forms.Keys.PageDown)
                    {
                        ListBox.FocusDown(ListBox?.DisplayableItems ?? 0);
                    }
                }

                if (e.KeyCode == System.Windows.Forms.Keys.Enter || e.KeyCode == System.Windows.Forms.Keys.Return)
                {
                    if (ListBox.Visible && ListBox.FocusIndex>=0)       // if we are showing list and there is a focus, we use that
                    {
                        ListBox.SelectCurrentFocus();
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine($"Autocomplete cancelled due to return");
                        CancelAutoComplete();                   // close any list box, and select this text, may be empty
                        SelectedEntry?.Invoke(this);
                    }
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseWheel(GLMouseEventArgs)"/>
        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (!e.Handled && ListBox.Visible)
            {
                if (e.Delta > 0)
                    ListBox.FocusUp();
                else
                    ListBox.FocusDown();
            }
        }

        private PolledTimer initialdelaytimer = new PolledTimer();
        private PolledTimer autocompleteinuitimer = new PolledTimer();
        private AutoResetEvent AutocompleteUIDone = new AutoResetEvent(false);
        private PolledTimer autocompletefinishedtimer = new PolledTimer();
        private string autocompletestring;
        private bool executingautocomplete = false;
        private bool ignoreautocomplete = false;
        private bool restartautocomplete = false;
        private SortedSet<string> autocompletestrings = null;
        private System.Threading.Thread ThreadAutoComplete;
        private bool endbuttonon = false;

        #endregion
    }
}
