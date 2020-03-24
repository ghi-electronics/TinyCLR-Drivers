using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.VlsiSolution.VS1053B
{
    public class VS1053B
    {
        private readonly SpiController spi;
        private readonly GpioPin dreq;
        private readonly GpioPin reset;

        private readonly SpiConnectionSettings dataSetting;
        private readonly SpiConnectionSettings cmdSetting;

        // Values
        const ushort SM_SDINEW = 0x800;
        const ushort SM_RESET = 0x04;

        // Registers
        const int SCI_MODE = 0x00;
        const int SCI_VOL = 0x0B;
        const int SCI_CLOCKF = 0x03;

        private byte[] block = new byte[32];
        private byte[] cmdBuffer = new byte[4];

        public VS1053B(SpiController spi, GpioPin dreq, GpioPin reset, GpioPin dataChipSelect, GpioPin commandChipSelect) {
            this.dataSetting = new SpiConnectionSettings() {
                ChipSelectType = SpiChipSelectType.Gpio,
                ChipSelectLine = dataChipSelect,
                ClockFrequency = 2000000,
                Mode = SpiMode.Mode0,
                ChipSelectActiveState = false

            };

            this.cmdSetting = new SpiConnectionSettings() {
                ChipSelectType = SpiChipSelectType.Gpio,
                ChipSelectLine = commandChipSelect,
                ClockFrequency = 2000000,
                Mode = SpiMode.Mode0,
                ChipSelectActiveState = false
            };

            this.reset = reset;
            this.reset.SetDriveMode(GpioPinDriveMode.Output);

            this.dreq = dreq;
            this.dreq.SetDriveMode(GpioPinDriveMode.InputPullUp);

            this.spi = spi;

            this.Reset();

            this.CommandWrite(SCI_MODE, SM_SDINEW);
            this.CommandWrite(SCI_CLOCKF, 0x98 << 8);
            this.CommandWrite(SCI_VOL, 0x0101);  // highest volume -1

            if (this.CommandRead(SCI_VOL) != (0x0101)) {
                throw new Exception("Failed to initialize MP3 Decoder.");
            }
        }

        private void Reset() {
            this.reset.Write( GpioPinValue.Low);
            Thread.Sleep(1);
            this.reset.Write(GpioPinValue.High);
            Thread.Sleep(100);
        }

        private void CommandWrite(byte address, ushort data) {
            while (this.dreq.Read() == GpioPinValue.Low)
                Thread.Sleep(1);

            var spiCmd = this.spi.GetDevice(this.cmdSetting);

            this.cmdBuffer[0] = 0x02;
            this.cmdBuffer[1] = address;
            this.cmdBuffer[2] = (byte)(data >> 8);
            this.cmdBuffer[3] = (byte)data;
            spiCmd.Write(this.cmdBuffer);
        }

        private ushort CommandRead(byte address) {
            ushort temp;

            while (this.dreq.Read() == GpioPinValue.Low)
                Thread.Sleep(1);

            var spiCmd = this.spi.GetDevice(this.cmdSetting);

            this.cmdBuffer[0] = 0x03;
            this.cmdBuffer[1] = address;
            this.cmdBuffer[2] = 0;
            this.cmdBuffer[3] = 0;

            var read = new byte[4];

            spiCmd.TransferFullDuplex(this.cmdBuffer, 0, this.cmdBuffer.Length, read, 0, read.Length);

            temp = read[2];
            temp <<= 8;
            temp += read[3];

            return temp;
        }

        public void SetVolume(byte left_channel, byte right_channel) => this.CommandWrite(SCI_VOL, (ushort)((255 - left_channel) << 8 | (255 - right_channel)));

        public void SendData(byte[] data) {
            var size = data.Length - data.Length % 32;

            var spiData = this.spi.GetDevice(this.dataSetting);
            
            for (var i = 0; i < size; i += 32) {
                while (this.dreq.Read() == GpioPinValue.Low)
                    Thread.Sleep(1);

                Array.Copy(data, i, this.block, 0, 32);
                spiData.Write(this.block);
            }
        }
    }
}
