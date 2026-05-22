using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebSiteDev
{
    public partial class ScalableUserControl : UserControl
    {
        private const float MinScale = 0.8f;

        private Size OriginalSize;
        private bool Initialized = false;
        private bool IsScaling = false;

        private readonly Dictionary<Control, Rectangle> OriginalBounds = new Dictionary<Control, Rectangle>();
        private readonly Dictionary<Control, float> OriginalFontSizes = new Dictionary<Control, float>();
        private readonly Dictionary<Control, float> AppliedFontSizes = new Dictionary<Control, float>();
        private readonly Dictionary<Control, Font> CreatedFonts = new Dictionary<Control, Font>();

        private readonly List<RelativeControlRule> RelativeRules = new List<RelativeControlRule>();

        // Таймер
        private Timer ResizeDebounceTimer;

        private float LastScaleX = 0f;
        private float LastScaleY = 0f;

        private const int WM_SETREDRAW = 11;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);

        public ScalableUserControl()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Load += ScalableUserControl_Load;
            this.Resize += ScalableUserControl_Resize;

            ResizeDebounceTimer = new Timer();
            ResizeDebounceTimer.Interval = 4;
            ResizeDebounceTimer.Tick += ResizeDebounceTimer_Tick;
        }

        

        private void ScalableUserControl_Load(object sender, EventArgs e)
        {
            if (Initialized)
            {
                return;
            }

            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            Initialized = true;
            OriginalSize = this.ClientSize;

            SaveBoundsRecursive(this);
        }

        private void SaveBoundsRecursive(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                bool isUserControl = control is UserControl;

                control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                control.Dock = DockStyle.None;

                OriginalBounds[control] = new Rectangle(control.Location, control.Size);

                if (control.Font != null)
                {
                    OriginalFontSizes[control] = control.Font.Size;
                }

                if (!isUserControl && control.Controls.Count > 0)
                {
                    SaveBoundsRecursive(control);
                }
            }
        }

        protected void ChangeControlOriginalLocation(Control control, Point NewOriginalLocation)
        {
            if (OriginalBounds.ContainsKey(control))
            {
                Rectangle orig = OriginalBounds[control];
                OriginalBounds[control] = new Rectangle(NewOriginalLocation, orig.Size);

                float ScaleX = (float)this.ClientSize.Width / OriginalSize.Width;
                float ScaleY = (float)this.ClientSize.Height / OriginalSize.Height;

                if (ScaleX < MinScale)
                {
                    ScaleX = MinScale;
                }

                if (ScaleY < MinScale)
                {
                    ScaleY = MinScale;
                }

                control.Location = new Point(
                    (int)Math.Round(NewOriginalLocation.X * ScaleX),
                    (int)Math.Round(NewOriginalLocation.Y * ScaleY)
                );
            }
            else
            {
                control.Location = NewOriginalLocation;
            }
        }

        protected void AddRightCenterRule(Control MovingControl, Control TargetControl, int spacing)
        {
            var rule = new RelativeControlRule
            {
                MovingControl = MovingControl,
                TargetControl = TargetControl,
                RuleType = RelativeRuleType.RightCenter,
                Spacing = spacing
            };
            RelativeRules.Add(rule);
            ApplyRelativeRule(rule);
        }

        protected void AddBottomCenterRule(Control MovingControl, Control TargetControl, int spacing)
        {
            var rule = new RelativeControlRule
            {
                MovingControl = MovingControl,
                TargetControl = TargetControl,
                RuleType = RelativeRuleType.BottomCenter,
                Spacing = spacing
            };
            RelativeRules.Add(rule);
            ApplyRelativeRule(rule);
        }

        protected void AddTopRightControlRule(Control MovingControl, int RightPadding, int topPadding)
        {
            var rule = new RelativeControlRule
            {
                MovingControl = MovingControl,
                TargetControl = this,
                RuleType = RelativeRuleType.ControlTopRight,
                RightPadding = RightPadding,
                TopPadding = topPadding
            };
            RelativeRules.Add(rule);
            ApplyRelativeRule(rule);
        }

        protected void RefreshRelativeRules()
        {
            ApplyRelativeRules();
        }

        private void ApplyRelativeRules()
        {
            if (RelativeRules.Count == 0)
            {
                return;
            }

            for (int i = 0; i < RelativeRules.Count; i++)
            {
                ApplyRelativeRule(RelativeRules[i]);
            }
        }

        private void ApplyRelativeRule(RelativeControlRule rule)
        {
            if (rule.MovingControl == null || rule.TargetControl == null)
            {
                return;
            }

            if (rule.MovingControl.IsDisposed || rule.TargetControl.IsDisposed)
            {
                return;
            }

            if (rule.RuleType == RelativeRuleType.RightCenter)
            {
                rule.MovingControl.Left = rule.TargetControl.Right + rule.Spacing;
                rule.MovingControl.Top = rule.TargetControl.Top + (rule.TargetControl.Height - rule.MovingControl.Height) / 2;
            }
            else if (rule.RuleType == RelativeRuleType.BottomCenter)
            {
                rule.MovingControl.Left = rule.TargetControl.Left + (rule.TargetControl.Width - rule.MovingControl.Width) / 2;
                rule.MovingControl.Top = rule.TargetControl.Bottom + rule.Spacing;
            }
            else if (rule.RuleType == RelativeRuleType.ControlTopRight)
            {
                rule.MovingControl.Left = this.ClientSize.Width - rule.MovingControl.Width - rule.RightPadding;
                rule.MovingControl.Top = rule.TopPadding;
            }
        }

        /// <summary>
        /// При изменении размера не масштабируем сразу перезапуск таймера
        /// </summary>
        private void ScalableUserControl_Resize(object sender, EventArgs e)
        {
            if (!Initialized || IsScaling)
            {
                return;
            }

            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0)
            {
                return;
            }

            if (ResizeDebounceTimer == null)
            {
                return;
            }

            ResizeDebounceTimer.Stop();
            ResizeDebounceTimer.Start();
        }

        /// <summary>
        /// Таймер дотикал масштабирование больше не будет откладываться и изменится масштаб
        /// </summary>
        private void ResizeDebounceTimer_Tick(object sender, EventArgs e)
        {
            if (ResizeDebounceTimer != null)
            {
                ResizeDebounceTimer.Stop();
            }

            if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
            {
                return;
            }

            PerformScale();
        }

        /// <summary>
        /// Основной метод масштабирования
        /// Защищён от вызова на уничтоженном контроле и от повторного входа
        /// </summary>
        private void PerformScale()
        {
            if (IsScaling || OriginalBounds.Count == 0)
            {
                return;
            }

            if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
            {
                return;
            }

            float ScaleX = (float)this.ClientSize.Width / OriginalSize.Width;
            float ScaleY = (float)this.ClientSize.Height / OriginalSize.Height;

            if (ScaleX < MinScale)
            {
                ScaleX = MinScale;
            }
            if (ScaleY < MinScale)
            {
                ScaleY = MinScale;
            }

            const float ScaleDelta = 0.005f;
            if (Math.Abs(ScaleX - LastScaleX) < ScaleDelta && Math.Abs(ScaleY - LastScaleY) < ScaleDelta)
            {
                return;
            }

            LastScaleX = ScaleX;
            LastScaleY = ScaleY;

            IsScaling = true;
            bool redrawFrozen = false;

            try
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    SendMessage(this.Handle, WM_SETREDRAW, false, 0);
                    redrawFrozen = true;
                }

                this.SuspendLayout();

                float FontScale = ScaleX;
                if (ScaleY < FontScale)
                {
                    FontScale = ScaleY;
                }

                foreach (var kvp in OriginalBounds)
                {
                    Control control = kvp.Key;
                    Rectangle orig = kvp.Value;

                    // Если контрол был уничтожен пропускаем
                    if (control == null || control.IsDisposed)
                    {
                        continue;
                    }

                    int NewX = (int)Math.Round(orig.X * ScaleX);
                    int NewY = (int)Math.Round(orig.Y * ScaleY);
                    int NewW = (int)Math.Round(orig.Width * ScaleX);
                    int NewH = (int)Math.Round(orig.Height * ScaleY);

                    control.SetBounds(NewX, NewY, NewW, NewH);

                    if (OriginalFontSizes.ContainsKey(control))
                    {
                        float origFontSize = OriginalFontSizes[control];
                        float newSize = origFontSize * FontScale;

                        if (newSize >= 1.0f && control.Font != null)
                        {
                            bool needUpdate = true;

                            if (AppliedFontSizes.ContainsKey(control))
                            {
                                if (Math.Abs(AppliedFontSizes[control] - newSize) < 0.5f)
                                {
                                    needUpdate = false;
                                }
                            }

                            if (needUpdate)
                            {
                                Font newFont = new Font(control.Font.FontFamily, newSize, control.Font.Style);

                                if (CreatedFonts.ContainsKey(control))
                                {
                                    CreatedFonts[control].Dispose();
                                    CreatedFonts.Remove(control);
                                }

                                CreatedFonts.Add(control, newFont);
                                control.Font = newFont;
                                AppliedFontSizes[control] = newSize;
                            }
                        }
                    }
                }

                ApplyRelativeRules();
            }
            finally // Выполняется в любом случае
            {
                this.ResumeLayout(false);

                if (redrawFrozen && this.IsHandleCreated && !this.IsDisposed)
                {
                    SendMessage(this.Handle, WM_SETREDRAW, true, 0);
                    this.Invalidate(true);
                }

                IsScaling = false;
            }

            OnScaledResize();
        }

        protected virtual void OnScaledResize()
        {

        }

        private enum RelativeRuleType
        {
            RightCenter,
            BottomCenter,
            ControlTopRight
        }

        private class RelativeControlRule
        {
            public Control MovingControl;
            public Control TargetControl;
            public RelativeRuleType RuleType;
            public int Spacing;
            public int RightPadding;
            public int TopPadding;
        }
    }
}