using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Uart;

namespace GHIElectronics.TinyCLR.Drivers.HiLetgo.Bluetooth {
    public class BluetoothController {
        public UartController serialPort;
        private GpioPin reset;
        private GpioPin statusInt;
        private Thread readerThread;

        private object lockobj = new object();

        private Client client;

        private Host host;


        /// <summary>Gets a value that indicates whether the bluetooth connection is connected.</summary>
		public bool IsConnected => this.statusInt.Read() == GpioPinValue.High;

        /// <summary>Sets Bluetooth module to work in Client mode.</summary>
        public Client ClientMode {
            get {
                lock (this.lockobj) {
                    if (this.host != null) throw new InvalidOperationException("Cannot use both Client and Host modes for Bluetooth module");
                    if (this.client == null) this.client = new Client(this);
                    return this.client;
                }
            }
        }

        /// <summary>Sets Bluetooth module to work in Host mode.</summary>
        public Host HostMode {
            get {
                lock (this.lockobj) {
                    if (this.client != null) throw new InvalidOperationException("Cannot use both Client and Host modes for Bluetooth module");
                    if (this.host == null) this.host = new Host(this);
                    return this.host;
                }
            }
        }

        /// <summary>Possible states of the Bluetooth module</summary>
        public enum BluetoothState {

            /// <summary>Module is initializing</summary>
            Initializing = 0,

            /// <summary>Module is ready</summary>
            Ready = 1,

            /// <summary>Module is in pairing mode</summary>
            Inquiring = 2,

            /// <summary>Module is making a connection attempt</summary>
            Connecting = 3,

            /// <summary>Module is connected</summary>
            Connected = 4,

            /// <summary>Module is diconnected</summary>
            Disconnected = 5
        }

        /// <summary>Constructs a new instance.</summary>
		/// <param name="socketNumber">The socket that this module is plugged in to.</param>
		public BluetoothController(UartController serialPort, GpioPin resetPin, GpioPin interruptPin, int baudrate) {
            // This finds the Socket instance from the user-specified socket number. This will generate user-friendly error messages if the socket is invalid. If there is more than one socket on this
            // module, then instead of "null" for the last parameter, put text that identifies the socket to the user (e.g. "S" if there is a socket type S)
            this.serialPort = serialPort;
            this.reset = resetPin;
            this.statusInt = interruptPin;



            this.serialPort.Enable();

            this.reset.Write(GpioPinValue.Low);

            Thread.Sleep(5);

            this.reset.Write(GpioPinValue.High);

            //this.SetDeviceBaud(baud);
            //this.serialPort.Flush();
            //this.serialPort.Close();
            //this.serialPort.BaudRate = (int)baud;
            //this.serialPort.Open();

            this.readerThread = new Thread(new ThreadStart(this.RunReaderThread));
            this.readerThread.Start();
            Thread.Sleep(500);
        }

        /// <summary>Hard Reset Bluetooth module</summary>
		public void Reset() {
            this.reset.Write(GpioPinValue.Low);
            Thread.Sleep(5);
            this.reset.Write(GpioPinValue.High);
        }

        /// <summary>Sets the device name as seen by other devices</summary>
		/// <param name="name">Name of the device</param>
		public void SetDeviceName(string name) => this.Write("\r\n+STNA=" + name + "\r\n");

        /// <summary>Switch the device to the directed speed</summary>
		/// <param baud="number">Name of the device</param>
		public void SetDeviceBaud(long baud) {
            var cmd = string.Empty;
            switch (baud) {
                case 9600:
                    cmd = "9600";
                    break;

                case 19200:
                    cmd = "19200";
                    break;

                case 38400:
                    cmd = "38400";
                    break;

                case 57600:
                    cmd = "57600";
                    break;

                case 115200:
                    cmd = "115200";
                    break;

                case 230400:
                    cmd = "230400";
                    break;

                case 460800:
                    cmd = "460800";
                    break;

                default:
                    cmd = "";
                    break;
            }

            if (cmd != "")
                this.Write("\r\n+STBD=" + cmd + "\r\n");
            //todo: check it is working?! Probably should check the return code and do something about it. in the meantime,
            Thread.Sleep(500);
        }

        /// <summary>Sets the PIN code for the Bluetooth module</summary>
		/// <param name="pinCode"></param>
		public void SetPinCode(string pinCode) => this.Write("\r\n+STPIN=" + pinCode + "\r\n");

        /// <summary>Thread that continuously reads incoming messages from the module, parses them and triggers the corresponding events.</summary>
        private void RunReaderThread() {

            while (true) {
                var response = "";
                while (this.serialPort.BytesToRead > 0) {
                    var c = new byte[1];

                    this.serialPort.Read(c, 0, c.Length);

                    response = response + (char)c[0];
                }
                if (response.Length > 0) {


                    //Check Bluetooth State Changed
                    if (response.IndexOf("+BTSTATE:") > -1) {
                        var atCommand = "+BTSTATE:";

                        //String parsing
                        // Return format: +COPS:<mode>[,<format>,<oper>]
                        var first = response.IndexOf(atCommand) + atCommand.Length;
                        var last = response.IndexOf("\n", first);
                        var state = int.Parse(((response.Substring(first, last - first)).Trim()));

                        this.OnBluetoothStateChanged(this, (BluetoothState)state);
                    }
                    //Check Pin Requested
                    if (response.IndexOf("+INPIN") > -1) {
                        // EDUARDO : Needs testing
                        this.OnPinRequested(this);
                    }
                    if (response.IndexOf("+RTINQ") > -1) {
                        //EDUARDO: Needs testing

                        var atCommand = "+RTINQ=";
                        //String parsing
                        var first = response.IndexOf(atCommand) + atCommand.Length;
                        var mid = response.IndexOf(";", first);
                        var last = response.IndexOf("\r", first);

                        // Keep reading until the end of the message
                        while (last < 0) {
                            while (this.serialPort.BytesToRead > 0) {
                                var c = new byte[1];

                                this.serialPort.Read(c, 0, c.Length);

                                response = response + (char)c[0];
                            }
                            last = response.IndexOf("\r", first);
                        }

                        var address = ((response.Substring(first, mid - first)).Trim());

                        var name = (response.Substring(mid + 1, last - mid));

                        this.OnDeviceInquired(this, address, name);
                        //Debug.Print("Add: " + address + ", Name: " + name );
                    }
                    else {
                        this.OnDataReceived(this, response);
                    }
                }
                Thread.Sleep(1);  //poundy changed from thread.sleep(10)
            }
        }

        private BluetoothStateChangedHandler onBluetoothStateChanged;

        /// <summary>Represents the delegate used for the <see cref="BluetoothStateChanged" /> event.</summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="btState">Current state of the Bluetooth module</param>
        public delegate void BluetoothStateChangedHandler(BluetoothController sender, BluetoothState btState);

        /// <summary>Event raised when the bluetooth module changes its state.</summary>
        public event BluetoothStateChangedHandler BluetoothStateChanged {
            add => this.onBluetoothStateChanged += value;

            remove {
                if (this.onBluetoothStateChanged != null) { this.onBluetoothStateChanged -= value; }
            }
        }

        /// <summary>Raises the <see cref="BluetoothStateChanged" /> event.</summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="btState">Current state of the Bluetooth module</param>
        protected virtual void OnBluetoothStateChanged(BluetoothController sender, BluetoothState btState) => this.onBluetoothStateChanged?.Invoke(sender, btState);

        private DataReceivedHandler onDataReceived;

        /// <summary>Represents the delegate used for the <see cref="DataReceived" /> event.</summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="data">Data received from the Bluetooth module</param>
        public delegate void DataReceivedHandler(BluetoothController sender, string data);

        /// <summary>Event raised when the bluetooth module changes its state.</summary>
        public event DataReceivedHandler DataReceived {
            add => this.onDataReceived += value;

            remove {
                if (this.onDataReceived != null) { this.onDataReceived -= value; }
            }
        }

        /// <summary>Raises the <see cref="DataReceived" /> event.</summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="data">Data string received by the Bluetooth module</param>
        protected virtual void OnDataReceived(BluetoothController sender, string data) => this.onDataReceived?.Invoke(sender, data);

        private PinRequestedHandler onPinRequested;

        /// <summary>Represents the delegate used for the <see cref="PinRequested" /> event.</summary>
        /// <param name="sender">The object that raised the event.</param>
        public delegate void PinRequestedHandler(BluetoothController sender);

        /// <summary>Event raised when the bluetooth module changes its state.</summary>
        public event PinRequestedHandler PinRequested {
            add => this.onPinRequested += value;

            remove {
                if (this.onPinRequested != null) { this.onPinRequested -= value; }
            }
        }

        /// <summary>Raises the <see cref="PinRequested" /> event.</summary>
        /// <param name="sender">The object that raised the event.</param>
        protected virtual void OnPinRequested(BluetoothController sender) => this.onPinRequested?.Invoke(sender);

        private DeviceInquiredHandler onDeviceInquired;

        /// <summary>Represents the delegate used for the <see cref="DeviceInquired" /> event.</summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="macAddress">MAC Address of the inquired device</param>
        /// <param name="name">Name of the inquired device</param>
        public delegate void DeviceInquiredHandler(BluetoothController sender, string macAddress, string name);

        /// <summary>Event raised when the bluetooth module changes its state.</summary>
        public event DeviceInquiredHandler DeviceInquired {
            add => this.onDeviceInquired += value;

            remove {
                if (this.onDeviceInquired != null) { this.onDeviceInquired -= value; }
            }
        }

        /// <summary>Raises the <see cref="PinRequested" /> event.</summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="macAddress">MAC Address of the inquired device</param>
        /// <param name="name">Name of the inquired device</param>
        protected virtual void OnDeviceInquired(BluetoothController sender, string macAddress, string name) => this.onDeviceInquired?.Invoke(sender, macAddress, name);
        public void Write(string text) {
            var data = Encoding.UTF8.GetBytes(text);

            this.serialPort.Write(data, 0, data.Length);
        }

        public void WriteLine(string text) {
            var data = Encoding.UTF8.GetBytes(text + "\n");

            this.serialPort.Write(data, 0, data.Length);
        }
    }
}
