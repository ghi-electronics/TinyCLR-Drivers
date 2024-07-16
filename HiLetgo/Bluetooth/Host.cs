using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace GHIElectronics.TinyCLR.Drivers.HiLetgo.Bluetooth {
    public class Host {
        private BluetoothController bluetooth;

        internal Host(BluetoothController bluetooth) {
            //Debug.Print("Host mode");
            this.bluetooth = bluetooth;
            this.bluetooth.Write("\r\n+STWMOD=1\r\n");
        }

        /// <summary>Starts inquiring for devices</summary>
        public void InquireDevice() =>
            //Debug.Print("Inquiring device");
            this.bluetooth.Write("\r\n+INQ=1\r\n");

        /// <summary>Makes a connection with a device using its MAC address.</summary>
        /// <param name="macAddress">MAC address of the device</param>
        public void Connect(string macAddress) =>
            //Debug.Print("Connecting to: " + macAddress);
            this.bluetooth.Write("\r\n+CONN=" + macAddress + "\r\n");

        /// <summary>Inputs the PIN code.</summary>
        /// <param name="pinCode">PIN code. Default 0000</param>
        public void InputPinCode(string pinCode) =>
            //Debug.Print("Inputting pin: " + pinCode);
            this.bluetooth.Write("\r\n+RTPIN=" + pinCode + "\r\n");

        /// <summary>Closes the current connection. Doesn't work yet.</summary>
        public void Disconnect() {
            //Debug.Print("Disconnection is not working...");
            //NOT WORKING
            // Documentation states that in order to disconnect, we pull PIO0 HIGH,
            // but this pin is not available in the socket... (see schematics)
        }
    }
}
