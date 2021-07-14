namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx
{
    public static class Mcp23Xxx
    {
        public static class ExternalGpioPin
        {
            public const string Id = "MicroChip.GpioExtender.Mcp23xxx.GpioController\\0";
            public const int GpA0 = 0;
            public const int GpA1 = 1;
            public const int GpA2 = 2;
            public const int GpA3 = 3;
            public const int GpA4 = 4;
            public const int GpA5 = 5;
            public const int GpA6 = 6;
            public const int GpA7 = 7;
            public const int GpB0 = 8;
            public const int GpB1 = 9;
            public const int GpB2 = 10;
            public const int GpB3 = 11;
            public const int GpB4 = 12;
            public const int GpB5 = 13;
            public const int GpB6 = 14;
            public const int GpB7 = 15;
        }

        public enum Product
        {
            Mcp23X08,
            Mcp23X09,
            Mcp23X17,
            Mcp23X18,
        }
    }
}
