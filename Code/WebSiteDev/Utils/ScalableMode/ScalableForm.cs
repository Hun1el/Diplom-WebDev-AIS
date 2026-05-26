using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WebSiteDev
{
    public partial class ScalableForm : Form
    {
        protected virtual float MinScale => 0.8f;
        protected virtual float MaxScale => float.MaxValue;

        private Size OriginalSize;
        private bool Initialized = false;
        private bool IsScaling = false;

        private readonly Dictionary<Control, Rectangle> OriginalBounds = new Dictionary<Control, Rectangle>();
        private readonly Dictionary<Control, float> OriginalFontSizes = new Dictionary<Control, float>();

        private readonly List<RelativeControlRule> RelativeRules = new List<RelativeControlRule>();

        public ScalableForm()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Load += ScalableForm_Load;
            this.Resize += ScalableForm_Resize;
        }

        private void ScalableForm_Load(object sender, EventArgs e)
        {
            if (Initialized)
            {
                return;
            }

            Initialized = true;
            OriginalSize = this.ClientSize;

            float minScale = MinScale;
            this.MinimumSize = new Size(
                (int)Math.Round(this.Size.Width * minScale),
                (int)Math.Round(this.Size.Height * minScale)
            );

            if (MaxScale < float.MaxValue)
            {
                this.MaximumSize = new Size(
                    (int)Math.Round(this.Size.Width * MaxScale),
                    (int)Math.Round(this.Size.Height * MaxScale)
                );
            }

            SaveBoundsRecursive(this);
        }

        private Size PreMaximizeSize;
        private Point PreMaximizeLocation;
        private bool IsPseudoMaximized = false;

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;

            if (m.Msg == WM_SYSCOMMAND && m.WParam.ToInt32() == SC_MAXIMIZE && MaxScale < float.MaxValue)
            {
                if (!IsPseudoMaximized)
                {
                    PreMaximizeSize = this.Size;
                    PreMaximizeLocation = this.Location;

                    this.Size = this.MaximumSize;

                    Rectangle screen = Screen.FromControl(this).WorkingArea;
                    this.Location = new Point(
                        screen.Left + (screen.Width - this.Width) / 2,
                        screen.Top + (screen.Height - this.Height) / 2
                    );

                    IsPseudoMaximized = true;
                }
                else
                {
                    this.Size = PreMaximizeSize;
                    this.Location = PreMaximizeLocation;
                    IsPseudoMaximized = false;
                }
                return;
            }

            base.WndProc(ref m);
        }

        private void SaveBoundsRecursive(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                control.Dock = DockStyle.None;

                OriginalBounds[control] = new Rectangle(control.Location, control.Size);

                if (control.Font != null)
                {
                    OriginalFontSizes[control] = control.Font.Size;
                }

                if (control.Controls.Count > 0)
                {
                    SaveBoundsRecursive(control);
                }
            }
        }

        /// <summary>
        /// Регистрирует контрол созданный после загрузки формы чтобы он тоже масштабировался
        /// </summary>
        protected void RegisterDynamicControl(Control control, bool recursive = true)
        {
            if (control == null)
            {
                return;
            }

            if (!Initialized || OriginalSize.Width <= 0 || OriginalSize.Height <= 0)
            {
                // Форма ещё не готова сохраняем как есть
                OriginalBounds[control] = new Rectangle(control.Location, control.Size);

                if (control.Font != null)
                {
                    OriginalFontSizes[control] = control.Font.Size;
                }
            }
            else
            {
                float currentScaleX = (float)this.ClientSize.Width / OriginalSize.Width;
                float currentScaleY = (float)this.ClientSize.Height / OriginalSize.Height;

                if (currentScaleX < MinScale)
                {
                    currentScaleX = MinScale;
                }

                if (currentScaleY < MinScale)
                {
                    currentScaleY = MinScale;
                }

                if (currentScaleX > MaxScale && MaxScale < float.MaxValue)
                {
                    currentScaleX = MaxScale;
                }

                if (currentScaleY > MaxScale && MaxScale < float.MaxValue)
                {
                    currentScaleY = MaxScale;
                }

                float fontScale = Math.Min(currentScaleX, currentScaleY);

                Rectangle current = new Rectangle(control.Location, control.Size);
                Rectangle original = new Rectangle(
                    (int)Math.Round(current.X / currentScaleX),
                    (int)Math.Round(current.Y / currentScaleY),
                    (int)Math.Round(current.Width / currentScaleX),
                    (int)Math.Round(current.Height / currentScaleY)
                );

                OriginalBounds[control] = original;

                if (control.Font != null)
                {
                    OriginalFontSizes[control] = control.Font.Size / fontScale;
                }
            }

            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            control.Dock = DockStyle.None;

            if (recursive && control.Controls.Count > 0)
            {
                foreach (Control child in control.Controls)
                {
                    RegisterDynamicControl(child, true);
                }
            }
        }

        /// <summary>
        /// Удаляет контрол из системы масштабирования (например, перед уничтожением старых карточек).
        /// </summary>
        protected void UnregisterControl(Control control, bool recursive = true)
        {
            if (control == null)
            {
                return;
            }

            if (OriginalBounds.ContainsKey(control))
            {
                OriginalBounds.Remove(control);
            }

            if (OriginalFontSizes.ContainsKey(control))
            {
                OriginalFontSizes.Remove(control);
            }

            if (recursive)
            {
                foreach (Control child in control.Controls)
                {
                    UnregisterControl(child, true);
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

                float minScale = MinScale;

                if (ScaleX < minScale)
                {
                    ScaleX = minScale;
                }

                if (ScaleY < minScale)
                {
                    ScaleY = minScale;
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
            RelativeControlRule rule = new RelativeControlRule();
            rule.MovingControl = MovingControl;
            rule.TargetControl = TargetControl;
            rule.RuleType = RelativeRuleType.RightCenter;
            rule.Spacing = spacing;

            RelativeRules.Add(rule);
            ApplyRelativeRule(rule);
        }

        protected void AddBottomCenterRule(Control MovingControl, Control TargetControl, int spacing)
        {
            RelativeControlRule rule = new RelativeControlRule();
            rule.MovingControl = MovingControl;
            rule.TargetControl = TargetControl;
            rule.RuleType = RelativeRuleType.BottomCenter;
            rule.Spacing = spacing;

            RelativeRules.Add(rule);
            ApplyRelativeRule(rule);
        }

        protected void AddTopRightFormRule(Control MovingControl, int RightPadding, int topPadding)
        {
            RelativeControlRule rule = new RelativeControlRule();
            rule.MovingControl = MovingControl;
            rule.TargetControl = this;
            rule.RuleType = RelativeRuleType.FormTopRight;
            rule.RightPadding = RightPadding;
            rule.TopPadding = topPadding;

            RelativeRules.Add(rule);
            ApplyRelativeRule(rule);
        }

        protected void RefreshRelativeRules()
        {
            ApplyRelativeRules();
        }

        private void ApplyRelativeRules()
        {
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
            else if (rule.RuleType == RelativeRuleType.FormTopRight)
            {
                rule.MovingControl.Left = this.ClientSize.Width - rule.MovingControl.Width - rule.RightPadding;
                rule.MovingControl.Top = rule.TopPadding;
            }
        }

        private void ScalableForm_Resize(object sender, EventArgs e)
        {
            if (!Initialized || IsScaling || OriginalBounds.Count == 0)
            {
                return;
            }

            if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0)
            {
                return;
            }

            IsScaling = true;
            this.SuspendLayout();

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

            float maxScale = MaxScale;

            if (ScaleX > maxScale && maxScale < float.MaxValue)
            {
                ScaleX = maxScale;
            }

            if (ScaleY > maxScale && maxScale < float.MaxValue)
            {
                ScaleY = maxScale;
            }

            float FontScale = Math.Min(ScaleX, ScaleY);

            foreach (var kvp in OriginalBounds)
            {
                Control control = kvp.Key;
                Rectangle orig = kvp.Value;

                // FlowLayoutPanel сам управляет расположением элементов
                bool isInFlowPanel = control.Parent is FlowLayoutPanel;

                if (!isInFlowPanel)
                {
                    control.Location = new Point(
                        (int)Math.Round(orig.X * ScaleX),
                        (int)Math.Round(orig.Y * ScaleY)
                    );
                }

                control.Size = new Size(
                    (int)Math.Round(orig.Width * ScaleX),
                    (int)Math.Round(orig.Height * ScaleY)
                );

                if (OriginalFontSizes.ContainsKey(control))
                {
                    float origFontSize = OriginalFontSizes[control];
                    float newSize = origFontSize * FontScale;

                    if (newSize >= 1.0f)
                    {
                        control.Font = new Font(control.Font.FontFamily, newSize, control.Font.Style);
                    }
                }
            }

            ApplyRelativeRules();

            this.ResumeLayout(false);
            IsScaling = false;

            OnScaledResize();
        }

        protected virtual void OnScaledResize()
        {

        }

        private enum RelativeRuleType
        {
            RightCenter,
            BottomCenter,
            FormTopRight
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