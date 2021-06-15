using System;

namespace GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets {

    public interface ISocketImpl : IDisposable {
        EndPoint LocalEndPoint { get; }
        EndPoint RemoteEndPoint { get; }
        int ReceiveTimeout { get; set; }
        int SendTimeout { get; set; }

        void Bind(EndPoint localEP);
        void Connect(EndPoint remoteEP);
        void Listen();
        ISocketImpl Accept();
        int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags);
        int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP);
        byte[] Receive(SocketFlags socketFlags);
        byte[] Receive(SocketFlags socketFlags, int timeout);
        int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags);
        int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, int timeout);
        byte[] ReceiveFrom(SocketFlags socketFlags, ref EndPoint remoteEP);
        byte[] ReceiveFrom(SocketFlags socketFlags, ref EndPoint remoteEP, int timeout);
        int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP);
        int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, int timeout);
        int BytesAvailable { get; }
        void Close();
    }
}
