using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace TinyCLRApplication13
{
    public class AT25M02Controller
    {
        private GpioPin chipSelect, hold, writeProtect;        
        private SpiDevice spi;
        
        public AT25M02Controller(SpiController spiController, GpioPin chipSelect, GpioPin hold = null, GpioPin writeProtect = null)
        {
            var setting = new SpiConnectionSettings()
            {                
                Mode = SpiMode.Mode0,
                ClockFrequency = 4000000,
                ChipSelectType = SpiChipSelectType.None,               
            };
            
            this.spi = spiController.GetDevice(setting);

            this.chipSelect = chipSelect;
            this.chipSelect.SetDriveMode(GpioPinDriveMode.Output);

            this.hold = hold;
            this.hold?.SetDriveMode(GpioPinDriveMode.Output);

            this.writeProtect = writeProtect;
            this.writeProtect?.SetDriveMode(GpioPinDriveMode.Output);

            hold?.Write(GpioPinValue.High);
            writeProtect?.Write(GpioPinValue.High);
        }
       
        public void Read(uint readAddress, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException();

            this.chipSelect.Write(GpioPinValue.Low);

            this.SendCommand(Command.Read);
            this.SendCommand((Command)((readAddress & 0xFF0000) >> 16));
            this.SendCommand((Command)((readAddress & 0x00FF00) >> 8));
            this.SendCommand((Command)(readAddress & 0x0000FF)); 

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = this.ReadData();
            }

            this.chipSelect.Write(GpioPinValue.High);
        }

        private byte ReadData()
        {
            var rx_data = new byte[1];
            this.spi.TransferFullDuplex(new[] { (byte)Command.Empty }, rx_data);
            return rx_data[0];
        }        

        public void Write(uint writeAddress, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException();

            if (data.Length > 256)
                throw new ArgumentOutOfRangeException("Max 256 bytes.");

            // Set the WEL bit
            this.Execute(Command.WriteEnable);

            this.chipSelect.Write(GpioPinValue.Low);

            this.SendCommand(Command.Write);
            this.SendCommand((Command)((writeAddress & 0xFF0000) >> 16));
            this.SendCommand((Command)((writeAddress & 0x00FF00) >> 8));
            this.SendCommand((Command)(writeAddress & 0x0000FF));

            this.spi.Write(data);

            this.chipSelect.Write(GpioPinValue.High);
        }

        private void Execute(Command command)
        {
            this.chipSelect.Write(GpioPinValue.Low);
            this.spi.Write(new[] { (byte)command });
            this.chipSelect.Write(GpioPinValue.High);
        }

        private void SendCommand(Command command) => this.spi.Write(new[] { (byte)command });

        public enum Command
        {
            Empty = 0xFF,
            Read = 0x03,
            Write = 0x02,
            WriteDisable = 0x04,
            ReadStatus = 0x05,
            WriteEnable = 0x06,
        }
    }
}
