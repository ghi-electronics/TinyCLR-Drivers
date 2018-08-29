using System;
using System.Drawing;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.ShijiLighting.APA102C {
    public class APA102C {
        private readonly SpiDevice spiBus;
        private readonly byte[] startFrame;
        private readonly byte[] stopFrame;
        private readonly byte[] ledFrame;
        private readonly int pixelCount;

        public APA102C(int pixelCount, string spiId, int chipselect) {
            this.pixelCount = pixelCount;
            this.startFrame = new byte[4];
            this.stopFrame = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            this.ledFrame = new byte[this.pixelCount * 4];
            
            // Initializes frame buffer for active LED frame data
            for (var i = 0; i < this.ledFrame.Length; i += 4) 
                this.ledFrame[i] = 0xE0;

            var spiSettings = new SpiConnectionSettings(chipselect) {
                Mode = SpiMode.Mode0,
                ClockFrequency = 1_200_000,
                DataBitLength = 8,
                UseControllerChipSelect = false
            };

            this.spiBus = SpiController.FromName(spiId).GetDevice(new SpiConnectionSettings(chipselect) { Mode = SpiMode.Mode0, ClockFrequency = 1_200_000, DataBitLength = 8, UseControllerChipSelect = false });
        }

        /// <param name="pixelIndex">The pixel in the chain to draw</param>
        /// <param name="pixelColor">A struct that has an individual color field for Red, Green, and Blue with a range of 0-255 each.</param>
        /// <param name="pixelIntensity">The level from 0 (off) to 31 (highest brightness)</param>
        public void SetLed(int pixelIndex, Pen pixelColor, int pixelIntensity) {
            if (this.pixelCount < pixelIndex)
                throw new ArgumentOutOfRangeException();

            var ledFrameIndex = pixelIndex * 4; // Positions index to beginning of each LED frame

            pixelIntensity |= 0x7 << 5;

            this.ledFrame[ledFrameIndex] = (byte)pixelIntensity;
            this.ledFrame[ledFrameIndex + 1] = pixelColor.Color.B;
            this.ledFrame[ledFrameIndex + 2] = pixelColor.Color.G;
            this.ledFrame[ledFrameIndex + 3] = pixelColor.Color.R;
        }

        public void RefreshLeds() {
            // The startFrame sends all zeros first in the chain to let the addressable LEDs know the next set of data is the LED Frame Data field.
            this.spiBus.Write(this.startFrame);
            // ledFrame is the entire pixel chain data.
            this.spiBus.Write(this.ledFrame);

            this.spiBus.Write(this.stopFrame);
        }
    }
}
