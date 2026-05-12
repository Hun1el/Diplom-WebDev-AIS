using System;
using System.Collections.Generic;
using System.Drawing;
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

        private readonly List<RelativeControlRule> RelativeRules = new List<RelativeControlRule>();

        public ScalableUserControl()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Load += ScalableUserControl_Load;
            this.Resize += ScalableUserControl_Resize;
        }

        private void ScalableUserControl_Load(object sender, EventArgs e)
        {
            if (Initialized)
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

        private void ScalableUserControl_Resize(object sender, EventArgs e)
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

            float FontScale = Math.Min(ScaleX, ScaleY);

            foreach (var kvp in OriginalBounds)
            {
                Control control = kvp.Key;
                Rectangle orig = kvp.Value;

                control.Location = new Point(
                    (int)Math.Round(orig.X * ScaleX),
                    (int)Math.Round(orig.Y * ScaleY)
                );

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