namespace GHIElectronics.TinyCLR.Drivers.BasicNet {

    // Generic abstraction to identify network addresses

    /// <summary>
    /// Provides a constructor and methods for creating network connection points (endpoints) and serializing endpoint information.<br/>
    /// <strong>IMPORTANT: Use this class only with WIZnet W5100 Ethernet TCP/IP Chip.</strong>
    /// </summary>
    public abstract class EndPoint {
        /// <summary>
        /// Creates a new instance of the EndPoint class from serialized information. 
        /// </summary>
        /// <param name="socketAddress">A SocketAddress object that stores the endpoint's information in a serial format.</param>
        /// <returns>A new EndPoint object that is initialized from the specified SocketAddress object.</returns>
        public abstract EndPoint Create(SocketAddress socketAddress);

    }; // abstract class EndPoint

}
