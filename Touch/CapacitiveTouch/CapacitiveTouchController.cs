using System;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Signals;

namespace GHIElectronics.TinyCLR.Drivers.Touch.CapacitiveTouch {
    public class CapacitiveTouchController {
        private int level;
        private PulseFeedback pulseFeedback;
        public double CalibrateMinValue { get; set; } = 0.008;
        public double CalibrateMaxValue { get; set; } = 0.015;

        /// <summary>
        /// Capacitive Touch constructor
        /// </summary>
        /// <param name="touchPin"> Gpio pin</param>
        /// <param name="sensitiveLevel">Sensitive level [0..100].</param>
        public CapacitiveTouchController(GpioPin touchPin, int sensitiveLevel) {
            if (sensitiveLevel < 0 || sensitiveLevel > 100)
                throw new ArgumentException("Level must be in range [0,100]");

            this.level = 100 - sensitiveLevel;

            this.pulseFeedback = new PulseFeedback(touchPin,
                PulseFeedbackMode.DrainDuration) {

                DisableInterrupts = false,
                Timeout = TimeSpan.FromSeconds(1),
                PulseLength = TimeSpan.FromTicks(10000),
                PulseValue = GpioPinValue.High,
            };

        }

        public double RawValue => this.pulseFeedback.Trigger().TotalMilliseconds;
        public bool IsTouched {
            get {
                var scale = Scale(this.RawValue * 10000, (int)(this.CalibrateMinValue * 10000), (int)(this.CalibrateMaxValue * 10000), 0, 100);

                if (scale >= this.level)
                    return true;

                else return false;
            }

        }

        static int Scale(double value, int originalMin, int originalMax, int scaleMin, int scaleMax) {
            var scale = (double)(scaleMax - scaleMin) / (originalMax - originalMin);
            var ret = (int)(scaleMin + ((value - originalMin) * scale));

            return ret > scaleMax ? scaleMax : (ret < scaleMin ? scaleMin : ret);
        }
    }
}
