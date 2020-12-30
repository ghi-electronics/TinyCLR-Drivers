using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Pwm;

namespace GHIElectronics.TinyCLR.Drivers.Motor.Servo {
    public class ServoController {
        private enum ServoType {
            Positional,
            Continuous
        }

        private PwmChannel servo;
        private PwmController controller;

        private ServoType type;

        private bool invertServo;
        private double minPulseLength;
        private double maxPulseLength;

        public ServoController(PwmController pwmController, PwmChannel pwnChannel) {

            this.invertServo = false;
            this.servo = pwnChannel;
            this.controller = pwmController;

            this.ConfigurePulseParameters(0.5, 2.4);
            this.ConfigureAsPositional(false);

            this.EnsureFrequency();
        }


        public void ConfigureAsPositional(bool inverted) {
            this.type = ServoType.Positional;
            this.invertServo = inverted;
        }

        public void ConfigureAsContinuous(bool inverted) {
            this.type = ServoType.Continuous;
            this.invertServo = inverted;
        }

        public void ConfigurePulseParameters(double minimumPulseWidth, double maximumPulseWidth) {
            if (minimumPulseWidth > 1.5 || minimumPulseWidth < 0.1) throw new ArgumentOutOfRangeException("Must be between 0.1 and 1.5 ms");
            if (maximumPulseWidth > 3 || maximumPulseWidth < 1.6) throw new ArgumentOutOfRangeException("Must be between 1.6 and 3 ms");

            this.minPulseLength = minimumPulseWidth;
            this.maxPulseLength = maximumPulseWidth;
        }

        public void Set(double value) {
            if (this.type == ServoType.Positional)
                this.FixedSetPosition(value);
            else
                this.ContiniousSetSpeed(value);
        }

        private void FixedSetPosition(double position) {
            if (position < 0 || position > 180) throw new ArgumentOutOfRangeException("degrees", "degrees must be between 0 and 180.");

            this.EnsureFrequency();// in case we used the other stuff. remove when we fix PWM controllers

            if (this.invertServo == true)
                position = 180 - position;

            // Typically, with 50 hz, 0 degree is 0.05 and 180 degrees is 0.10
            //double duty = ((position / 180.0) * (0.10 - 0.05)) + 0.05;
            var duty = ((position / 180.0) * (this.maxPulseLength / 20 - this.minPulseLength / 20)) + this.minPulseLength / 20;

            this.servo.SetActiveDutyCyclePercentage(duty);
            this.servo.Start();
        }

        private void ContiniousSetSpeed(double speed) {
            if (speed < -100 || speed > 100) throw new ArgumentOutOfRangeException("speed", "degrees must be between -100 and 100.");

            speed += 100;
            var d = speed / 200.0 * 180;
            this.FixedSetPosition(d);
        }

        private void EnsureFrequency() => this.controller.SetDesiredFrequency(1 / 0.020);

        public void Stop() => this.servo.Stop();
    }
}
