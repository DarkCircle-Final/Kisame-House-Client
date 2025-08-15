using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Behaviors
{
    public class SnapSliderBehavior : Behavior<Slider>
    {
        public double StepSize { get; set; } = 1.0;

        protected override void OnAttachedTo(Slider slider)
        {
            base.OnAttachedTo(slider);
            slider.ValueChanged += OnValueChanged;
        }

        protected override void OnDetachingFrom(Slider slider)
        {
            base.OnDetachingFrom(slider);
            slider.ValueChanged -= OnValueChanged;
        }

        private void OnValueChanged(object? sender, ValueChangedEventArgs e)
        {
            if (sender is not Slider s || StepSize <= 0) return;

            double snapped = Math.Round((e.NewValue - s.Minimum) / StepSize) * StepSize + s.Minimum;

            if (Math.Abs(snapped - e.NewValue) > 0.0001)
                s.Value = snapped;
        }
    }
}
