using System;
using System.Diagnostics;
using GHIElectronics.TinyCLR.Devices.Can;

namespace GHIElectronics.TinyCLR.Drivers.Scania.J1939 {
    public delegate void AlarmDelegate(ushort spn, byte fmi);
    public class J1939
    {
        //Subscribe to this delegate to receive a event when a engine alarm was received
        public AlarmDelegate alarmReceived;

        //Contains all the arbitration ID this parser can understand
        //Very useful to access when creating your CAN filter!
        #region Arbitration IDs

        public const uint HighResTotalFuelId = 0x00FD0900;
        public const uint RpmId = 0x00F00400;
        public const uint TotalFuelId = 0x00FEE900;
        public const uint CoolantTemperatureId = 0x00FEEE00;
        public const uint LoadId = 0x00F00300;
        public const uint TotalEngineHoursId = 0x00FEE500;
        public const uint FuelRateId = 0x00FEF200;
        public const uint OilPressureId = 0x00FEEF00;
        public const uint SingleAlarmId = 0x00FECA00;
        public const uint BatteryVoltageId = 0x00FEF700;

        #endregion

        //Contains all the parsed engine variables
        //Access them here after parsing messages
        #region Engine Data Variables

        public int TotalFuelHighRes { get; set; }
        public int RPM { get; set; }
        public int TotalFuel { get; set; }
        public int CoolantTemperature { get; set; }
        public int Load { get; set; }
        public int TotalEngineHours { get; set; }
        public int FuelRate { get; set; }
        public int OilPressure { get; set; }
        public int OilTemperature { get; set; }
        public int BatteryVoltage { get; set; }

        #endregion

        //The timestamp of when the last message was received
        //Very useful to determine if the engine is still on
        public static DateTime LastJ1939MessageTime;

        /*
         * Use this function to parse your J1939 messages.
         * It takes in any CanMessage object retreived from the CAN controller inside your SITCore chip
         * It filters out messages that are not recognized, so you can put it every message you receive if you wish
         * Access the Engine Data Variables after parsing to get your data out
         */
        public void Parse(CanMessage canMessage)
        {
            //Only take out the part that identifies the message within J1939
            var messageId = canMessage.ArbitrationId & 0x00FFFF00;

            Debug.WriteLine(canMessage.ArbitrationId.ToString("X8"));

            //Compare the masked id with the IDs we know
            //If we get a match, get it's value and then store it in the right variable
            if (messageId == HighResTotalFuelId)
                this.TotalFuelHighRes = GetTotalHighResFuel(canMessage);
            else if (messageId == RpmId)
                this.RPM = GetRPM(canMessage);
            else if (messageId == TotalFuelId)
                this.TotalFuel = GetTotalFuel(canMessage);
            else if (messageId == CoolantTemperatureId)
            {
                this.CoolantTemperature = GetCoolantTemperature(canMessage);
                this.OilTemperature = GetOilTemperature(canMessage);
            }
            else if (messageId == LoadId)
                this.Load = GetLoad(canMessage);
            else if (messageId == TotalEngineHoursId)
                this.TotalEngineHours = GetTotalEngineHours(canMessage);
            else if (messageId == FuelRateId)
                this.FuelRate = GetFuelRate(canMessage);
            else if (messageId == OilPressureId)
                this.OilPressure = GetOilPressure(canMessage);
            else if (messageId == BatteryVoltageId)
                this.BatteryVoltage = GetBatteryVoltage(canMessage);
            else if (messageId == SingleAlarmId) {
                //If we received an alarm, we should raise a delegate event
                this.alarmReceived?.Invoke(GetSpn(canMessage), GetFmi(canMessage));
            }
            else {
                //We should return. The captured message was NOT a J1939 message we recognize.
                return;
            }

            //Set the time we received an engine message
            LastJ1939MessageTime = DateTime.Now;
        }

        /*
         * These methods are used to actually convert the J1939 bytes into readable and usable values
         * These methods can be accessed directly, but can also be accessed through use of the J1939.Parse() method
         */
        #region CAN To Variable

        //High res fuel in liters, 0 decimals
        public static int GetTotalHighResFuel(CanMessage message) => (message.Data[4] + (message.Data[5] << 8) + (message.Data[6] << 16) + (message.Data[7] << 24)) / 1000;

        //RPM, 0 decimals
        public static ushort GetRPM(CanMessage message) => (ushort)((message.Data[3] + (message.Data[4] << 8)) / 8);

        //Total fuel in liters, 0 decimals
        public static int GetTotalFuel(CanMessage message) => (message.Data[4] + (message.Data[5] << 8) + (message.Data[6] << 16) + (message.Data[7] << 24)) / 2;

        //Coolant temperature in degrees celsius, 0 decimals
        public static byte GetCoolantTemperature(CanMessage message) => message.Data[0];

        //Load in %, 0 decimals
        public static byte GetLoad(CanMessage message) => message.Data[2];

        //Total time the engine has run in hours, 0 decimals
        public static int GetTotalEngineHours(CanMessage message) => (message.Data[0] + (message.Data[1] << 8) + (message.Data[2] << 16) + (message.Data[3] << 24)) / 20;

        //The current fuel usage in liters per hour, 0 decimals
        public static ushort GetFuelRate(CanMessage message) => (ushort)((message.Data[0] + (message.Data[1] << 8)) / 20);

        //Oilpressure in kPa, 0 decimals
        public static ushort GetOilPressure(CanMessage message) => (ushort)(message.Data[3] * 4);

        //Oil temperature in degrees celsius, 0 decimals
        public static ushort GetOilTemperature(CanMessage message) => (ushort)(((message.Data[2] + (message.Data[3] << 8)) / 32) - 273);

        //Battery voltage in volts, 0,05V per bit (so multiply by 20 if you want the floating point voltage value)
        public static ushort GetBatteryVoltage(CanMessage message) => (ushort)(message.Data[6] + (message.Data[7] << 8));

        //Converts a canmessage into the SPN of an alarm
        public static ushort GetSpn(CanMessage message) => (ushort)((message.Data[3] << 8) + message.Data[2]);

        //Converts a canmessage into the FMI of an alarm
        public static byte GetFmi(CanMessage message) => message.Data[4];

        #endregion
    }
}
