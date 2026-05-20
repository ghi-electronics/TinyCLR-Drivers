//------------------------------------------------------------------------------
// Azure IoT Hub device connection string helper.
//
// Mirrors the parsing + per-device SAS-token + MQTT-username pieces that
// Microsoft's full DeviceClient.CreateFromConnectionString hides behind its
// SDK. The embedded MQTT library doesn't ship an Azure-specific client, so
// this class wraps the awkward bits so user code stays short:
//
//     var conn = IotHubConnectionString.Parse(connectionString);
//     mqttSettings.BrokerName = conn.HostName;
//     mqttSettings.BrokerPort = IotHubConnectionString.DefaultMqttPort;
//     connectSettings.ClientId = conn.DeviceId;
//     connectSettings.UserName = conn.GetMqttUserName();
//     connectSettings.Password = conn.BuildSasToken(TimeSpan.FromDays(1));
//------------------------------------------------------------------------------

using System;

namespace GHIElectronics.TinyCLR.Drivers.Azure.SAS
{
    /// <summary>
    /// Parsed Azure IoT Hub device connection string. Format:
    /// <c>HostName=&lt;hub&gt;.azure-devices.net;DeviceId=&lt;id&gt;;SharedAccessKey=&lt;key&gt;</c>.
    /// </summary>
    public class IotHubConnectionString
    {
        /// <summary>Standard Azure IoT Hub MQTT-over-TLS port.</summary>
        public const int DefaultMqttPort = 8883;

        /// <summary>
        /// MQTT API version required by current Azure IoT Hub endpoints.
        /// Embedded in the MQTT username — IoT Hub rejects CONNECT without it.
        /// </summary>
        public const string MqttApiVersion = "2021-04-12";

        /// <summary>IoT Hub hostname, e.g. <c>myhub.azure-devices.net</c>.</summary>
        public string HostName { get; set; }

        /// <summary>Device identifier registered in the IoT Hub.</summary>
        public string DeviceId { get; set; }

        /// <summary>The device's primary (or secondary) shared-access key, base64.</summary>
        public string SharedAccessKey { get; set; }

        /// <summary>
        /// Parse a connection string. Throws <see cref="ArgumentException"/>
        /// if any of <c>HostName</c>, <c>DeviceId</c>, or <c>SharedAccessKey</c>
        /// is missing. Unknown fields (e.g. <c>GatewayHostName</c>) are ignored.
        /// </summary>
        public static IotHubConnectionString Parse(string connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");

            var result = new IotHubConnectionString();
            var parts = connectionString.Split(';');
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var eq = part.IndexOf('=');
                if (eq <= 0) continue;
                var key = part.Substring(0, eq);
                var value = part.Substring(eq + 1);
                if (key == "HostName") result.HostName = value;
                else if (key == "DeviceId") result.DeviceId = value;
                else if (key == "SharedAccessKey") result.SharedAccessKey = value;
            }

            if (result.HostName == null || result.HostName.Length == 0)
                throw new ArgumentException("Connection string missing HostName.");
            if (result.DeviceId == null || result.DeviceId.Length == 0)
                throw new ArgumentException("Connection string missing DeviceId.");
            if (result.SharedAccessKey == null || result.SharedAccessKey.Length == 0)
                throw new ArgumentException("Connection string missing SharedAccessKey.");

            return result;
        }

        /// <summary>
        /// MQTT username Azure IoT Hub expects on CONNECT:
        /// <c>&lt;hub&gt;/&lt;deviceId&gt;/?api-version=2021-04-12</c>.
        /// </summary>
        public string GetMqttUserName()
            => this.HostName + "/" + this.DeviceId + "/?api-version=" + MqttApiVersion;

        /// <summary>
        /// Build a SAS token (the MQTT password) for this device that
        /// expires in <paramref name="timeToLive"/>. Device-scoped:
        /// target includes <c>/devices/&lt;id&gt;</c> and no policy KeyName.
        /// </summary>
        public string BuildSasToken(TimeSpan timeToLive)
        {
            return new SharedAccessSignatureBuilder
            {
                KeyName = null,
                Key = this.SharedAccessKey,
                Target = this.HostName + "/devices/" + this.DeviceId,
                TimeToLive = timeToLive,
            }.ToSignature();
        }
    }
}
