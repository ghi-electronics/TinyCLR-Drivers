using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using GHIElectronics.TinyCLR.Devices.Network;
using GHIElectronics.TinyCLR.Devices.Network.Provider;

namespace GHIElectronics.TinyCLR.Drivers.Atmel.Winc15x0 {
    public class Winc15x0Interface : INetworkControllerProvider {

        public enum PowerSave {
            PowerSave_None = 0,
            PowerSave_Automatic = 1,
            PowerSave_H_Automatic = 2,
            PowerSave_Deep_Automatic = 3,
            PowerSave_Manual = 4,
        }

        private readonly NetworkController networkController;

        public event NetworkLinkConnectedChangedEventHandler NetworkLinkConnectedChanged {
            add {
                this.networkController.NetworkLinkConnectedChanged += value;
            }

            remove {
                this.networkController.NetworkLinkConnectedChanged -= value;
            }
        }
        public event NetworkAddressChangedEventHandler NetworkAddressChanged {
            add {
                this.networkController.NetworkAddressChanged += value;
            }

            remove {
                this.networkController.NetworkAddressChanged -= value;
            }
        }

        public NetworkInterfaceType InterfaceType => this.networkController.InterfaceType;
        public NetworkCommunicationInterface CommunicationInterface => this.networkController.CommunicationInterface;

        public Winc15x0Interface() => this.networkController = NetworkController.FromName("GHIElectronics.TinyCLR.NativeApis.ATWINC15xx.NetworkController");

        ~Winc15x0Interface() => this.Dispose();

        public void Dispose() => GC.SuppressFinalize(this);

        public string[] Scan() {
            var response = this.NativeScan(out var numAp);

            // 44 bytes for each AP - refer to native tstrM2mWifiscanResult
            var ssids = new string[numAp];

            for (var i = 0; i < numAp; i++) {
                var ssid = new char[32];

                var index = 0;
                for (var index2 = i * 44 + 10; index < ssid.Length; index++) {
                    ssid[index] = (char)response[index2++];
                }

                ssids[i] = new string(ssid);
            }
            return ssids;
        }

        public string GetFirmwareVersion() {
            this.ReadFirmwareVersion(out var ver1, out var ver2);

            var major = (ver1 >> 16) & 0xFF;
            var monor = (ver1 >> 8) & 0xFF;
            var path = (ver1 >> 0) & 0xFF;

            return major.ToString() + "." + monor.ToString() + "." + path.ToString() + " Svnrev " + ver2.ToString();
        }

        public void Enable() => this.networkController.Enable();

        public void Disable() => this.networkController.Disable();

        public bool GetLinkConnected() => this.networkController.GetLinkConnected();

        public NetworkIPProperties GetIPProperties() => this.networkController.GetIPProperties();

        public NetworkInterfaceProperties GetInterfaceProperties() => this.networkController.GetInterfaceProperties();

        public void SetInterfaceSettings(NetworkInterfaceSettings settings) => this.networkController.SetInterfaceSettings(settings);

        public void SetCommunicationInterfaceSettings(NetworkCommunicationInterfaceSettings settings) => this.networkController.SetCommunicationInterfaceSettings(settings);

        public int Create(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) => this.networkController.Provider.Create(addressFamily, socketType, protocolType);

        public void Close(int socket) => this.networkController.Provider.Close(socket);

        public void Bind(int socket, SocketAddress address) => this.networkController.Provider.Bind(socket, address);

        public void Listen(int socket, int backlog) => this.networkController.Provider.Listen(socket, backlog);

        public int Accept(int socket) => this.networkController.Provider.Accept(socket);

        public void Connect(int socket, SocketAddress address) => this.networkController.Provider.Connect(socket, address);

        public int Available(int socket) => this.networkController.Provider.Available(socket);

        public bool Poll(int socket, int microSeconds, SelectMode mode) => this.networkController.Provider.Poll(socket, microSeconds, mode);

        public int Send(int socket, byte[] buffer, int offset, int count, SocketFlags flags) => this.networkController.Provider.Send(socket, buffer, offset, count, flags);

        public int Receive(int socket, byte[] buffer, int offset, int count, SocketFlags flags) => this.networkController.Provider.Receive(socket, buffer, offset, count, flags);

        public int SendTo(int socket, byte[] buffer, int offset, int count, SocketFlags flags, SocketAddress address) => this.networkController.Provider.SendTo(socket, buffer, offset, count, flags, address);

        public int ReceiveFrom(int socket, byte[] buffer, int offset, int count, SocketFlags flags, ref SocketAddress address) => this.networkController.Provider.ReceiveFrom(socket, buffer, offset, count, flags, ref address);

        public void GetRemoteAddress(int socket, out SocketAddress address) => this.networkController.Provider.GetRemoteAddress(socket, out address);

        public void GetLocalAddress(int socket, out SocketAddress address) => this.networkController.Provider.GetLocalAddress(socket, out address);

        public void GetOption(int socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) => this.networkController.Provider.GetOption(socket, optionLevel, optionName, optionValue);

        public void SetOption(int socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) => this.networkController.Provider.SetOption(socket, optionLevel, optionName, optionValue);

        public int AuthenticateAsClient(int socketHandle, string targetHost, X509Certificate rootCertificate, SslProtocols sslProtocols) => this.networkController.Provider.AuthenticateAsClient(socketHandle, targetHost, rootCertificate, sslProtocols);

        public int AuthenticateAsServer(int socketHandle, X509Certificate certificate, SslProtocols sslProtocols) => this.networkController.Provider.AuthenticateAsServer(socketHandle, certificate, sslProtocols);

        public int SecureRead(int handle, byte[] buffer, int offset, int count) => this.networkController.Provider.SecureRead(handle, buffer, offset, count);

        public int SecureWrite(int handle, byte[] buffer, int offset, int count) => this.networkController.Provider.SecureWrite(handle, buffer, offset, count);

        public void GetHostByName(string name, out string canonicalName, out SocketAddress[] addresses) => this.networkController.Provider.GetHostByName(name, out canonicalName, out addresses);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool TurnOn();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern byte[] NativeScan(out int numAp);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void SetPowerSave(PowerSave powerSave, int sleepDuration, int listenInterval, bool receiveBroadcast);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool FirmwareUpdatebyOta(string url, int timeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool FlashWrite(uint address, byte[] rawData, int offset, int count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool FlashRead(uint address, byte[] rawData, int offset, int count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool FlashErase(uint address, int count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern uint GetFlashSize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern uint ReadChipId();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ReadFirmwareVersion(out uint ver1, out uint ver2);
    }
}
