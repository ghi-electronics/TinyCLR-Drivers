using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace GHIElectronics.TinyCLR.Drivers.HiLetgo.Bluetooth {
    public class Client {
        private BluetoothController bluetooth;

        internal Client(BluetoothController bluetooth) {
            //Debug.Print("Client Mode");
            this.bluetooth = bluetooth;
            this.bluetooth.Write("\r\n+STWMOD=0\r\n");
        }

        /// <summary>Enters pairing mode</summary>
        public void EnterPairingMode() =>
            //Debug.Print("Enter Pairing Mode");
            this.bluetooth.Write("\r\n+INQ=1\r\n");

        /// <summary>Inputs pin code</summary>
        /// <param name="pinCode">Module's pin code. Default: 0000</param>
        public void InputPinCode(string pinCode) =>
            //Debug.Print("Inputting pin: " + pinCode);
            this.bluetooth.Write("\r\n+RTPIN=" + pinCode + "\r\n");

        /// <summary>Closes current connection. Doesn't work yet.</summary>
        public void Disconnect() {
            //Debug.Print("Disconnection is not working...");
            //NOT WORKING
            // Documentation states that in order to disconnect, we pull PIO0 HIGH,
            // but this pin is not available in the socket... (see schematics)
        }

        /// <summary>Sends data through the connection.</summary>
        /// <param name="message">String containing the data to be sent</param>
        public void Send(string message) =>
            //Debug.Print("Sending: " + message);
            this.bluetooth.Write(message);

        /// <summary>Sends data through the connection.</summary>
        /// <param name="message">String containing the data to be sent</param>
        public void SendLine(string message) =>
            //Debug.Print("Sending: " + message);
            this.bluetooth.WriteLine(message);
    }
}
