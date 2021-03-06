﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoClicker
{
    public partial class AutoClicker : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int RegisterHotKey(IntPtr hWnd, int id, uint modifier, uint vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        //ID for the F6, F7 Hotkey, used later to check which hotkey is being used
        private const int F6HOTKEYID = 1;
        private const int F7HOTKEYID = 2;

        //Constant value for the hotkey value using WndProc
        private const int WM_HOTKEY = 0x0312;

        //Modifiers for Hotkey registry
        private enum Modifiers
        {
            NONE = 0x0000,
            ALT = 0x0001,
            CONTROL = 0x0002,
            SHIFT = 0x0004,
            WINDOWS = 0x0008
        }

        //Separate thread to run the clicking operation
        private Thread ClickerThread;

        //Boolean to check if the clicker thread is running
        public static Boolean ClickerEnabled = false;

        //Mouse Speed Selectors
        private Boolean Slider;
        private Boolean ManualEntry;

        //Mouse Position Selectors
        private Boolean ActivePosition;
        private Boolean SetPosition;

        //Mouse Speed Options for manual entry
        private int Hours;
        private int Minutes;
        private int Seconds;
        private int Milliseconds;

        //Slider Speed
        private int SliderSpeed;

        //Mouse Position Options
        private int XPosition;
        private int YPosition;

        //Click Options
        private int MouseButton;
        private int ClickType;

        //Cursor position point
        private Point CursorPoint = new Point();

        //Total Milliseconds for the timer
        private int TotalMilliseconds;

        //Constructor to start the window and set defaults
        public AutoClicker()
        {
            InitializeComponent();

            this.KeyPreview = true;

            MouseButtonBox.SelectedIndex = 0;
            MouseButtonBox.DropDownStyle = ComboBoxStyle.DropDownList;

            ClickTypeBox.SelectedIndex = 0;
            ClickTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;

            ActivePosRadio.Checked = true;
            ManualRadio.Checked = true;

            ClearButton.BackColor = Color.LightBlue;

            StopButton.Enabled = false;
            StopButton.BackColor = Color.Empty;

            StartButton.BackColor = Color.LightGreen;

            XPos.Enabled = false;
            YPos.Enabled = false;

            SpeedSlider.Enabled = false;

            this.Slider = false;
            this.ManualEntry = true;

            this.ActivePosition = true;
            this.SetPosition = false;

            this.Hours = 0;
            this.Minutes = 0;
            this.Seconds = 0;
            this.Milliseconds = 0;

            this.SliderSpeed = 0;

            this.XPosition = 0;
            this.YPosition = 0;

            this.MouseButton = 0;
            this.ClickType = 0;
        }

        //Method to run when the Auto Clicker window loads
        private void AutoClickerFormLoad(object sender, EventArgs e)
        {
            //Register the F6 hotkey with the system
            RegisterHotKey(this.Handle, F6HOTKEYID, (uint)Modifiers.NONE, (uint)Keys.F6);

            //Register the F7 hotkey with the system
            RegisterHotKey(this.Handle, F7HOTKEYID, (uint)Modifiers.NONE, (uint)Keys.F7);
        }

        //Method to run when the Auto Clicker window closes
        private void AutoClickerFormClose(object sender, FormClosedEventArgs e)
        {
            //Unregister the F6 hotkey with the system
            UnregisterHotKey(this.Handle, F6HOTKEYID);

            //Unregister the F7 hotkey with the system
            UnregisterHotKey(this.Handle, F7HOTKEYID);
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            //Check for key down press
            if(m.Msg == WM_HOTKEY)
            {
                //Switch over the ID assigned
                switch (m.WParam.ToInt32())
                {
                    case F6HOTKEYID:
                        if (ClickerEnabled == true)
                        {
                            StopButton.PerformClick();
                        }
                        else if (ClickerEnabled == false)
                        {
                            StartButton.PerformClick();
                        }
                        break;

                    case F7HOTKEYID:
                        if (ClickerEnabled == false && SetPosRadio.Checked)
                        {
                            this.Cursor = new Cursor(Cursor.Current.Handle);
                            this.CursorPoint = new Point(Cursor.Position.X, Cursor.Position.Y);

                            this.XPosition = Cursor.Position.X;
                            this.YPosition = Cursor.Position.Y;

                            this.XPos.Text = Cursor.Position.X.ToString();
                            this.YPos.Text = Cursor.Position.Y.ToString();
                        }
                        break;
                }
            }

            base.WndProc(ref m);
        }

        private Boolean CheckValues()
        {
            if (!String.IsNullOrWhiteSpace(MillisecondsBox.Text))
                this.Milliseconds = Int32.Parse(MillisecondsBox.Text.ToString());

            if (!String.IsNullOrWhiteSpace(SecondsBox.Text))
                this.Seconds = Int32.Parse(SecondsBox.Text.ToString());

            if (!String.IsNullOrWhiteSpace(MinutesBox.Text))
                this.Minutes = Int32.Parse(MinutesBox.Text.ToString());

            if (!String.IsNullOrWhiteSpace(HoursBox.Text))
                this.Hours = Int32.Parse(HoursBox.Text.ToString());

            if(this.Milliseconds > 0 || this.Seconds > 0 || this.Minutes > 0 || this.Hours > 0 || this.SliderSpeed > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private Boolean CheckSetPosition()
        {
            if (this.SetPosition && !String.IsNullOrWhiteSpace(XPos.Text) && !String.IsNullOrWhiteSpace(YPos.Text))
            {
                if (!String.IsNullOrWhiteSpace(XPos.Text))
                    this.XPosition = Int32.Parse(XPos.Text.ToString());

                if (!String.IsNullOrWhiteSpace(YPos.Text))
                    this.YPosition = Int32.Parse(YPos.Text.ToString());

                this.CursorPoint.X = this.XPosition;
                this.CursorPoint.Y = this.YPosition;

                return true;
            }
            else if (this.ActivePosition)
            {
                return true;
            }

            return false;
        }

        private void StartAutoClicker(object sender, EventArgs e)
        {
            if(ClickerEnabled == false && CheckValues() && CheckSetPosition())
            {
                this.ActivePosition = ActivePosRadio.Checked;
                this.SetPosition = SetPosRadio.Checked;

                this.MouseButton = MouseButtonBox.SelectedIndex;
                this.ClickType = ClickTypeBox.SelectedIndex;

                ClickerThread = new Thread(new ThreadStart(AutoClickerEnableThread));
                ClickerThread.Start();

                ClickerEnabled = true;

                StartButton.BackColor = Color.Empty;
                StopButton.BackColor = Color.IndianRed;
                ClearButton.BackColor = Color.Empty;

                StartButton.Enabled = false;
                StopButton.Enabled = true;
                ClearButton.Enabled = false;

                ManualRadio.Enabled = false;
                SliderRadio.Enabled = false;
                ActivePosRadio.Enabled = false;
                SetPosRadio.Enabled = false;

                if (ManualRadio.Checked)
                {
                    HoursBox.Enabled = false;
                    MinutesBox.Enabled = false;
                    SecondsBox.Enabled = false;
                    MillisecondsBox.Enabled = false;

                    ManualRadio.Enabled = false;
                    SliderRadio.Enabled = false;
                    ActivePosRadio.Enabled = false;
                    SetPosRadio.Enabled = false;

                    MouseButtonBox.Enabled = false;
                    ClickTypeBox.Enabled = false;

                    if (SetPosRadio.Checked)
                    {
                        XPos.Enabled = false;
                        YPos.Enabled = false;
                    }
                }
                else if (SliderRadio.Checked)
                {
                    SpeedSlider.Enabled = false;

                    MouseButtonBox.Enabled = false;
                    ClickTypeBox.Enabled = false;

                    if (SetPosRadio.Checked)
                    {
                        XPos.Enabled = false;
                        YPos.Enabled = false;
                    }
                }
            }
        }

        private void AutoClickerEnableThread()
        {
            if (this.ManualEntry)
            {
                this.TotalMilliseconds = Milliseconds + (Seconds * 1000) + (Minutes * 60000) + (Hours * 3600000);

                _ = new AutoClickerDriver(this.TotalMilliseconds, this.MouseButton, this.ClickType, this.CursorPoint, this.ActivePosition, this.SetPosition, this.Slider, this.ManualEntry);
            }
            else if (this.Slider)
            {
                this.TotalMilliseconds = SliderSpeed;

                _ = new AutoClickerDriver(this.TotalMilliseconds, this.MouseButton, this.ClickType, this.CursorPoint, this.ActivePosition, this.SetPosition, this.Slider, this.ManualEntry);
            }
        }

        private void StopAutoClicker(object sender, EventArgs e)
        {
            if (ClickerEnabled)
            {
                ClickerThread.Abort();

                ClickerEnabled = false;

                StartButton.BackColor = Color.LightGreen;
                StopButton.BackColor = Color.Empty;
                ClearButton.BackColor = Color.LightBlue;

                StartButton.Enabled = true;
                StopButton.Enabled = false;
                ClearButton.Enabled = true;

                ManualRadio.Enabled = true;
                SliderRadio.Enabled = true;
                ActivePosRadio.Enabled = true;
                SetPosRadio.Enabled = true;

                if (ManualRadio.Checked)
                {
                    HoursBox.Enabled = true;
                    MinutesBox.Enabled = true;
                    SecondsBox.Enabled = true;
                    MillisecondsBox.Enabled = true;

                    ManualRadio.Enabled = true;
                    SliderRadio.Enabled = true;
                    ActivePosRadio.Enabled = true;
                    SetPosRadio.Enabled = true;

                    MouseButtonBox.Enabled = true;
                    ClickTypeBox.Enabled = true;

                    if (SetPosRadio.Checked)
                    {
                        XPos.Enabled = true;
                        YPos.Enabled = true;
                    }
                }
                else if (SliderRadio.Checked)
                {
                    SpeedSlider.Enabled = true;

                    MouseButtonBox.Enabled = true;
                    ClickTypeBox.Enabled = true;

                    if (SetPosRadio.Checked)
                    {
                        XPos.Enabled = true;
                        YPos.Enabled = true;
                    }
                }
            }
        }

        //Hotkeys for when the window is in focus
        private void HotKeys(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Escape)
            {
                if(ClickerEnabled == true)
                {
                    this.ClickerThread.Abort();
                    ClickerEnabled = false;
                }
                Application.Exit();
            }
            if(e.KeyCode == Keys.F8)
            {
                ClearButton.PerformClick();
            }
        }

        private void ClearEntryBoxes(object sender, EventArgs e)
        {
            HoursBox.Text = "";
            MinutesBox.Text = "";
            SecondsBox.Text = "";
            MillisecondsBox.Text = "";
            XPos.Text = "";
            YPos.Text = "";

            this.Milliseconds = 0;
            this.Seconds = 0;
            this.Minutes = 0;
            this.Hours = 0;

            this.XPosition = 0;
            this.YPosition = 0;

            MouseButtonBox.SelectedIndex = 0;
            ClickTypeBox.SelectedIndex = 0;
        }

        private void SetPositionRadioSelector(object sender, EventArgs e)
        {
            this.SetPosition = true;
            this.ActivePosition = false;
            XPos.Enabled = true;
            YPos.Enabled = true;
        }

        private void SetActiveRadioSelector(object sender, EventArgs e)
        {
            this.SetPosition = false;
            this.ActivePosition = true;
            XPos.Enabled = false;
            YPos.Enabled = false;
        }

        private void SetManualEntryRadioSelector(object sender, EventArgs e)
        {
            SpeedSlider.Enabled = false;
            HoursBox.Enabled = true;
            MinutesBox.Enabled = true;
            SecondsBox.Enabled = true;
            MillisecondsBox.Enabled = true;

            this.ManualEntry = true;
            this.Slider = false;

            this.SliderSpeed = 0;
        }

        private void SetSliderRadioButton(object sender, EventArgs e)
        {
            SpeedSlider.Enabled = true;
            HoursBox.Enabled = false;
            MinutesBox.Enabled = false;
            SecondsBox.Enabled = false;
            MillisecondsBox.Enabled = false;

            this.Slider = true;
            this.ManualEntry = false;

            this.SliderSpeed = 0;
        }

        private void SliderScroll(object sender, EventArgs e)
        {
            this.SliderSpeed = SpeedSlider.Value * 10;
            SliderSpeedValue.Text = this.SliderSpeed.ToString();
        }

        private void NumericValidation(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && (e.KeyChar != '\b'))
            {
                e.Handled = true;
            }
        }
    }
}
