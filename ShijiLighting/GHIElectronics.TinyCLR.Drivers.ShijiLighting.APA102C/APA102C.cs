using System;
using System.Collections;
using System.Text;
using System.Threading;

using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.ShijiLighting.APA102C {
    public class APA102C {
        private SpiController spiBusContoller;
        private SpiDevice spiBus;
        private readonly string spiID;
        private readonly SpiConnectionSettings spiSettings;
        private readonly byte[] startFrame;
        private readonly byte[] stopFrame;
        private byte[] ledFrame;
        private readonly int pixelCount;
        private int ledFrameIndex;
        private int pixelIntensity;

        public APA102C(int pixelCount, string spiID) {
            this.pixelCount = pixelCount;
            this.startFrame = new byte[4];                           // Initializes all elements in startFrame array to 0x0
            this.stopFrame = new byte[] { 0xff, 0xff, 0xff, 0xff };
            this.ledFrame = new byte[this.pixelCount * 4];           // Sets up 32 bit data buffer per each LED Frame (pixel)

            for (var i = 0; i < this.ledFrame.Length; i += 4)        // Initializes frame buffer to active LED frame data
            {
                this.ledFrame[i] = 0xE0;
            }

            this.spiSettings = new SpiConnectionSettings(7) {
                Mode = SpiMode.Mode0,
                ClockFrequency = 1_200_000,
                DataBitLength = 8
            };

            this.spiID = spiID;
            this.spiBusContoller = SpiController.FromName(this.spiID);
            this.spiBus = this.spiBusContoller.GetDevice(this.spiSettings);
        }

        /// <param name="pixelIndex">The pixel in the chain to draw</param>
        /// <param name="colorData16bit565">The 16 bit color to draw. It is in the RGB 565 format.</param>
        /// <param name="pixelIntensity">The level from 0 (off) to 31 (highest brightness)</param>
        public void SetLED(int pixelIndex, short colorData16bit565, int pixelIntensity = 1) {
            if (this.pixelCount < pixelIndex) {
                throw new Exception("Pixel Index is out of range of Pixel Count.");
            }

            this.ledFrameIndex = pixelIndex * 4; // Positions index to beginning of each LED frame

            this.pixelIntensity = pixelIntensity;
            this.pixelIntensity |= 0x7 << 5;

            this.ledFrame[this.ledFrameIndex] = (byte)this.pixelIntensity;
            this.ledFrame[this.ledFrameIndex + 1] = (byte)((colorData16bit565 & 0x1F) << 3);              // Blue byte
            this.ledFrame[this.ledFrameIndex + 2] = (byte)(((colorData16bit565 & 0x7E0) >> 5) << 3);      // Green byte
            this.ledFrame[this.ledFrameIndex + 3] = (byte)(((colorData16bit565 & 0xF800) >> 11) << 3);    // Red byte
        }

        public void RefreshLEDs() {
            // The startFrame sends all zeros first in the chain to let the addressable LEDs know the next set of data is the LED Frame Data field.
            this.spiBus.Write(this.startFrame);
            // ledFrame is the entire pixel chain data.
            this.spiBus.Write(this.ledFrame);
            // Unknown if this is necessary
            this.spiBus.Write(this.stopFrame);
        }
    }
}
