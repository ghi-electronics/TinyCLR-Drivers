﻿namespace GHIElectronics.TinyCLR.Drivers.STMicroelectronics.SPWF04Sx {
   
    public enum SPWF04SxHashType {
        SHA1,
        SHA224,
        SHA256,
        MD5,
    }

    public enum SPWF04SxMemVolumeType {
        ExtFlash,
        UserFlash,
        Ram,
        ApplFlash,
    }

    public enum SPWF04SxConnectionSecurityType {
        None,
        Tls,
    }

    public enum SPWF04SxConnectionType {
        Tcp,
        Udp,
    }

    public enum SPWF04SxWiFiState {
        HardwarePowerUp = 0,
        HardwareFailure = 1,
        RadioTerminatedByUser = 2,
        RadioIdle = 3,
        ScanInProgress = 4,
        ScanComplete = 5,
        JoinInProgress = 6,
        Joined = 7,
        AccessPointStarted = 8,
        HandshakeComplete = 9,
        ReadyToTransmit = 10,
    }

    public enum SPWF04SxCommandIds {
        AT = 0x01,
        HELP = 0x02,
        STS = 0x05,
        PEERS = 0x35,
        RESET = 0x03,
        PMS = 0x04,
        PYTHON = 0x08,
        GCFG = 0x09,
        SCFG = 0x0A,
        WCFG = 0x0B,
        FCFG = 0x0C,
        FSUPDATE = 0x58,
        FWUPDATE = 0x56,
        GPIOC = 0x13,
        GPIOR = 0x14,
        GPIOW = 0x15,
        DAC = 0x16,
        ADC = 0x17,
        PWM = 0x18,
        TIME = 0x11,
        RANDOM = 0x12,
        FSM = 0x21,
        FSU = 0x22,
        FSC = 0x23,
        FSD = 0x25,
        FSR = 0x26,
        FSL = 0x27,
        FSP = 0x28,
        HASH = 0x29,
        WPAECERT = 0x2A,
        TLSCERT = 0x2B,
        WPS = 0x36,
        WIFI = 0x32,
        SCAN = 0x33,
        SSIDTXT = 0x34,
        PING = 0x39,
        SOCKON = 0x41,
        SOCKQ = 0x42,
        SOCKC = 0x43,
        SOCKW = 0x44,
        SOCKR = 0x45,
        SOCKL = 0x46,
        SOCKDON = 0x47,
        SOCKDQ = 0x48,
        SOCKDC = 0x49,
        SOCKDW = 0x4A,
        SOCKDR = 0x4B,
        SOCKDL = 0x4C,
        WSOCKON = 0x61,
        WSOCKQ = 0x62,
        WSOCKC = 0x63,
        WSOCKW = 0x64,
        WSOCKR = 0x65,
        WSOCKL = 0x66,
        TFTPGET = 0x51,
        TFTPPUT = 0x52,
        SMTP = 0x53,
        HTTPGET = 0x54,
        HTTPPOST = 0x55,
        INPUTSSI = 0x59,
        MQTTCONN = 0x5A,
        MQTTSUB = 0x5B,
        MQTTPUB = 0x5C,
        MQTTUNSUB = 0x5D,
        MQTTDISC = 0x5E,
    }

    public enum SPWF04SxIndication {
        ConsoleActive = 0,
        PowerOn = 1,
        Reset = 2,
        WatchdogRunning = 3,
        LowMemory = 4,
        WiFiHardwareFailure = 5,
        ConfigurationFailure = 7,
        HardFault = 8,
        StackOverflow = 9,
        MallocFailed = 10,
        RadioStartup = 11,
        WiFiPSMode = 12,
        Copyright = 13,
        WiFiBssRegained = 14,
        WiFiSignalLow = 15,
        WiFiSignalOk = 16,
        BootMessages = 17,
        KeytypeNotImplemented = 18,
        WiFiJoin = 19,
        WiFiJoinFailed = 20,
        WiFiScanning = 21,
        ScanBlewUp = 22,
        ScanFailed = 23,
        WiFiUp = 24,
        WiFiAssociationSuccessful = 25,
        StartedAP = 26,
        APStartFailed = 27,
        StationAssociated = 28,
        DhcpReply = 29,
        WiFiBssLost = 30,
        WiFiException = 31,
        WiFiHardwareStarted = 32,
        WiFiNetwork = 33,
        WiFiUnhandledEvent = 34,
        WiFiScan = 35,
        WiFiUnhandledIndication = 36,
        //Reserved37 = 37,
        WiFiPoweredDown = 38,
        HWInMiniAPMode = 39,
        WiFiDeauthentication = 40,
        WiFiDisassociation = 41,
        WiFiUnhandledManagement = 42,
        WiFiUnhandledData = 43,
        WiFiUnknownFrame = 44,
        Dot11Illegal = 45,
        WpaCrunchingPsk = 46,
        //Reserved47 = 47,
        //Reserved48 = 48,
        WpaTerminated = 49,
        WpaStartFailed = 50,
        WpaHandshakeComplete = 51,
        GpioInterrupt = 52,
        Wakeup = 53,
        //Reserved54 = 54,
        PendingData = 55,
        InputToRemote = 56,
        OutputFromRemote = 57,
        SocketClosed = 58,
        //Reserved59 = 59,
        //Reserved60 = 60,
        IncomingSocketClient = 61,
        SocketClientGone = 62,
        SocketDroppingData = 63,
        RemoteConfiguration = 64,
        FactoryReset = 65,
        LowPowerMode = 66,
        GoingIntoStandby = 67,
        ResumingFromStandby = 68,
        GoingIntoDeepSleep = 69,
        ResumingFromDeepSleep = 70,
        //Reserved71 = 71,
        StationDisassociated = 72,
        SystemConfigurationUpdated = 73,
        RejectedFoundNetwork = 74,
        RejectedAssociation = 75,
        WiFiAuthenticationTimedOut = 76,
        WiFiAssociationTimedOut = 77,
        MicFailure = 78,
        //Reserved79 = 79,
        UdpBroadcast = 80,
        WpsGeneratedDhKeyset = 81,
        WpsEnrollmentAttemptTimedOut = 82,
        SockdDroppingClient = 83,
        NtpServerDelivery = 84,
        DhcpFailedToGetLease = 85,
        MqttPublished = 86,
        MqttClosed = 87,
        WebSocketData = 88,
        WebSocketClosed = 89,
        FileReceived = 90,
    }
}
