/* 
* NMEA parser for TinyCLR 2.0
* 
* Version 1.0 : 
* - Initial revision
* 
* Copyright 2020 MBNSoftware.Net 
* Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at 
* http://www.apache.org/licenses/LICENSE-2.0 
* Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
* either express or implied. See the License for the specific language governing permissions and limitations under the License. 
*/

using System;

namespace MBN.Modules
{
    public static class NMEAParser
    {
        #region Public enums
        public enum Talkers
        {
            BEIDOU1 = 0x4244,
            DSC = 0x4344,
            ECDIS = 0x4543,
            GALILEO = 0x4741,
            BEIDOU2 = 0x4742,
            GLONASS = 0x474C,
            MULTIPLE = 0x474E,
            GPS = 0x4750,
            INTEGRATED_INSTRUMENTATION = 0x4949,
            INTEGRATED_NAVIGATION = 0x494E,
            LORANC = 0x4C43,
            QZSS = 0x515A,
            NAVIC = 0x4749
        }
        #endregion

        #region Public structs
        public struct RMC
        {
            public Int16 TalkerID;
            public DateTime FixTime;
            public Char Status;
            public Double Latitude;
            public Char LatitudeHemisphere;
            public Double Longitude;
            public Char LongitudePosition;
            public Double SpeedKnots;
            public Double SpeedKm;
            public Double TrackAngle;
            public Double MagneticVariation;
            public Char MagneticVariationDirection;
            public Byte Checksum;
        }

        public struct GGA
        {
            public Int16 TalkerID;
            public DateTime FixTime;
            public Double Latitude;
            public Char LatitudeHemisphere;
            public Double Longitude;
            public Char LongitudePosition;
            public Byte QualityIndicator;
            public Byte SatellitesInUse;
            public Double HorizontalDilution;
            public Double AntennaAltitude;
            public Char AntennaAltitudeUnit;
            public Double GeoidalSeparation;
            public Char GeoidalSeparationUnit;
            public Double AgeOfDifferentialData;
            public Int32 DifferentialReferenceStationID;
            public Byte Checksum;
        }

        public struct GSA
        {
            public Int16 TalkerID;
            public Char SelectionMode;
            public Byte Mode;
            public Int32 Satellite1Id;
            public Int32 Satellite2Id;
            public Int32 Satellite3Id;
            public Int32 Satellite4Id;
            public Int32 Satellite5Id;
            public Int32 Satellite6Id;
            public Int32 Satellite7Id;
            public Int32 Satellite8Id;
            public Int32 Satellite9Id;
            public Int32 Satellite10Id;
            public Int32 Satellite11Id;
            public Int32 Satellite12Id;
            public Double PDOP;
            public Double HDOP;
            public Double VDOP;
            public Byte Checksum;
        }

        public struct GSV
        {
            public Int16 TalkerID;
            public Byte NumberOfSentences;
            public Byte SequenceNumber;
            public Byte SatellitesInView;
            public Int32 Satellite1Id;
            public Int32 Satellite1Elevation;
            public Int32 Satellite1Azimuth;
            public Byte Satellite1SNR;
            public Int32 Satellite2Id;
            public Int32 Satellite2Elevation;
            public Int32 Satellite2Azimuth;
            public Byte Satellite2SNR;
            public Int32 Satellite3Id;
            public Int32 Satellite3Elevation;
            public Int32 Satellite3Azimuth;
            public Byte Satellite3SNR;
            public Int32 Satellite4Id;
            public Int32 Satellite4Elevation;
            public Int32 Satellite4Azimuth;
            public Byte Satellite4SNR;
            public Byte Checksum;
        }

        public struct VTG
        {
            public Int16 TalkerID;
            public Double CourseOverGroundDegrees;
            public Double CourseOverGroundMagnetic;
            public Double SpeedOverGroundKnots;
            public Double SpeedOverGroundKm;
            public Byte Checksum;
        }

        public struct HDT
        {
            public Int16 TalkerID;
            public Double Heading;
            public Byte Checksum;
        }

        public struct GLL
        {
            public Int16 TalkerID;
            public Double Latitude;
            public Char LatitudeHemisphere;
            public Double Longitude;
            public Char LongitudePosition;
            public DateTime FixTime;
            public Char Status;
            public Byte Checksum;
        }
        #endregion

        #region Public vars
        public static RMC RMCSentence;
        public static GGA GGASentence; 
        public static GSA GSASentence;
        public static GSV[] GSVSentence;
        public static VTG VTGSentence;
        public static HDT HDTSentence;
        public static GLL GLLSentence;
        #endregion

        #region Constructor
        static NMEAParser()
        {
            RMCSentence = new RMC();
            GGASentence = new GGA();
            GSASentence = new GSA();
            GSVSentence = new GSV[3];
            VTGSentence = new VTG();
            HDTSentence = new HDT();
            GLLSentence = new GLL();
            lockGGA = new Object();
            lockGSA = new Object();
            lockRMC = new Object();
            lockGSV = new Object();
            lockVTG = new Object();
            lockHDT = new Object();
            lockGLL = new Object();
        }
        #endregion

        #region Private vars
        private static readonly Byte[] patternRMC = new Byte[3] { 82, 77, 67 };
        private static readonly Byte[] patternGGA = new Byte[3] { 71, 71, 65 };
        private static readonly Byte[] patternGSA = new Byte[3] { 71, 83, 65 };
        private static readonly Byte[] patternGSV = new Byte[3] { 71, 83, 86 };
        private static readonly Byte[] patternVTG = new Byte[3] { 86, 84, 71 };
        private static readonly Byte[] patternHDT = new Byte[3] { 72, 68, 84 };
        private static readonly Byte[] patternGLL = new Byte[3] { 71, 76, 76 };

        private static readonly Byte[][] SupportedPatterns = new Byte[][] { patternGGA, patternGSA, patternRMC, patternGSV, patternVTG, patternHDT, patternGLL };
        private static readonly Object lockGGA, lockGSA, lockRMC, lockGSV, lockVTG, lockHDT, lockGLL;

        private static Byte b0, b1;
        private static Int32 resultInt32;
        private static Int64 resultInt64;

        private static readonly Byte[] commas = new Byte[20];
        #endregion

        #region Private methods

        private static Int32 IntFromAscii(Byte[] bytes, Int32 startIndex, Int32 count)
        {
            if (bytes.Length == 0)
                return 0;
            resultInt32 = 0;
            for (Byte i = 0; i < count; ++i)
            {
                resultInt32 += (Int32)((bytes[i + startIndex] - 48) * Math.Pow(10, count - i - 1));
            }

            return resultInt32;
        }

        private static Double DoubleFromAscii(Byte[] bytes, Int32 startIndex, Int32 count)
        {
            if (bytes.Length == 0)
                return 0.0;
            
            resultInt64 = 0;
            var pow = 0;
            var dec = 0;
            for (var i = 0; i < count; ++i)
            {
                if (bytes[i + startIndex] != 46)
                {
                    resultInt64 += (Int64)((bytes[i + startIndex] - 48) * Math.Pow(10, count - pow - 1));
                    pow++;
                }
                else
                {
                    dec = i;
                }
            }

            return resultInt64 / Math.Pow(10, count - dec);
        }

        private static void FindCommas(Byte[] sentence)
        {
            var count = 0;
            for (Byte i = 0; i < sentence.Length; i++)
            {
                if (sentence[i] == 44)
                {
                    commas[count] = i;
                    count++;
                }
            }
        }

        #endregion

        #region private NMEA parsing methods
        private static void ParseRMC(Byte[] sentence)
        {
            FindCommas(sentence);
            
            lock (lockRMC)
            {
                RMCSentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                RMCSentence.FixTime = new DateTime(
                    IntFromAscii(sentence, commas[8] + 5, 2) + 2000,
                    IntFromAscii(sentence, commas[8] + 3, 2),
                    IntFromAscii(sentence, commas[8] + 1, 2),
                    IntFromAscii(sentence, 7, 2),
                    IntFromAscii(sentence, 9, 2),
                    IntFromAscii(sentence, 11, 2));
                RMCSentence.Status = (Char)sentence[14];
                RMCSentence.Latitude = DoubleFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1) / 100.0f;
                RMCSentence.LatitudeHemisphere = (Char)sentence[commas[3] + 1];
                RMCSentence.Longitude = DoubleFromAscii(sentence, commas[4] + 1, commas[5] - commas[4] - 1) / 100.0f;
                RMCSentence.LongitudePosition = (Char)sentence[commas[5] + 1];
                RMCSentence.SpeedKnots = DoubleFromAscii(sentence, commas[6] + 1, commas[7] - commas[6] - 1);
                RMCSentence.SpeedKm = RMCSentence.SpeedKnots * 1.852f;
                RMCSentence.TrackAngle = DoubleFromAscii(sentence, commas[7] + 1, commas[8] - commas[7] - 1);
                RMCSentence.MagneticVariation = DoubleFromAscii(sentence, commas[9] + 1, commas[10] - commas[9] - 1);
                RMCSentence.MagneticVariationDirection = (Char)sentence[sentence.Length - 4];
                
                b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                RMCSentence.Checksum = (Byte)((b0 << 4) + b1);
            }
        }

        
        private static void ParseGGA(Byte[] sentence)
        {
            FindCommas(sentence);

            lock (lockGGA)
            {
                GGASentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                GGASentence.FixTime = new DateTime(
                    DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                    IntFromAscii(sentence, 7, 2),
                    IntFromAscii(sentence, 9, 2),
                    IntFromAscii(sentence, 11, 2));
                GGASentence.Latitude = DoubleFromAscii(sentence, commas[1] + 1, commas[2] - commas[1] - 1) / 100.0f;
                GGASentence.LatitudeHemisphere = (Char)sentence[commas[2] + 1];
                GGASentence.Longitude = DoubleFromAscii(sentence, commas[3] + 1, commas[4] - commas[3] - 1) / 100.0f;
                GGASentence.LongitudePosition = (Char)sentence[commas[4] + 1];
                GGASentence.QualityIndicator = sentence[commas[5] + 1];
                GGASentence.SatellitesInUse = (Byte)IntFromAscii(sentence, commas[6] + 1, commas[7] - commas[6] - 1);
                GGASentence.HorizontalDilution = DoubleFromAscii(sentence, commas[8] + 1, commas[9] - commas[8] - 1);
                GGASentence.AntennaAltitude = DoubleFromAscii(sentence, commas[9] + 1, commas[10] - commas[9] - 1);
                GGASentence.AntennaAltitudeUnit = (Char)sentence[commas[10] + 1];
                GGASentence.GeoidalSeparation = DoubleFromAscii(sentence, commas[11] + 1, commas[12] - commas[11] - 1);
                GGASentence.GeoidalSeparationUnit = (Char)sentence[commas[12] + 1];
                GGASentence.AgeOfDifferentialData = DoubleFromAscii(sentence, commas[14] + 1, commas[14] - commas[13] - 1);
                GGASentence.DifferentialReferenceStationID = IntFromAscii(sentence, commas[14] + 1, commas[15] - commas[14] - 1);

                b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                GGASentence.Checksum = (Byte)((b0 << 4) + b1);
            }
        }

        private static void ParseGSA(Byte[] sentence)
        {
            FindCommas(sentence);
            lock (lockGSA)
            {
                GSASentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                GSASentence.SelectionMode = (Char)sentence[commas[0] + 1];
                GSASentence.Mode = sentence[commas[1] + 1];
                GSASentence.Satellite1Id = IntFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1);
                GSASentence.Satellite2Id = IntFromAscii(sentence, commas[3] + 1, commas[4] - commas[3] - 1);
                GSASentence.Satellite3Id = IntFromAscii(sentence, commas[4] + 1, commas[5] - commas[4] - 1);
                GSASentence.Satellite4Id = IntFromAscii(sentence, commas[5] + 1, commas[6] - commas[5] - 1);
                GSASentence.Satellite5Id = IntFromAscii(sentence, commas[6] + 1, commas[7] - commas[6] - 1);
                GSASentence.Satellite6Id = IntFromAscii(sentence, commas[7] + 1, commas[8] - commas[7] - 1);
                GSASentence.Satellite7Id = IntFromAscii(sentence, commas[8] + 1, commas[9] - commas[8] - 1);
                GSASentence.Satellite8Id = IntFromAscii(sentence, commas[9] + 1, commas[10] - commas[9] - 1);
                GSASentence.Satellite9Id = IntFromAscii(sentence, commas[10] + 1, commas[11] - commas[10] - 1);
                GSASentence.Satellite10Id = IntFromAscii(sentence, commas[11] + 1, commas[12] - commas[11] - 1);
                GSASentence.Satellite11Id = IntFromAscii(sentence, commas[12] + 1, commas[13] - commas[12] - 1);
                GSASentence.Satellite12Id = IntFromAscii(sentence, commas[13] + 1, commas[14] - commas[13] - 1);
                GSASentence.PDOP = DoubleFromAscii(sentence, commas[14] + 1, commas[15] - commas[14] - 1);
                GSASentence.HDOP = DoubleFromAscii(sentence, commas[15] + 1, commas[16] - commas[15] - 1);
                GSASentence.VDOP = DoubleFromAscii(sentence, commas[16] + 1, sentence.Length - commas[16] - 4);

                b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                GSASentence.Checksum = (Byte)((b0 << 4) + b1);
            }
        }
        
        private static void ParseGSV(Byte[] sentence)
        {
            FindCommas(sentence);
            var Seq = sentence[9] - 49;
            lock (lockGSV)
            {
                GSVSentence[Seq].TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                GSVSentence[Seq].NumberOfSentences = (Byte)(sentence[7] - 48);
                GSVSentence[Seq].SequenceNumber = (Byte)(Seq + 1);
                GSVSentence[Seq].SatellitesInView = (Byte)IntFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1);

                GSVSentence[Seq].Satellite1Id = IntFromAscii(sentence, commas[3] + 1, commas[4] - commas[3] - 1);
                GSVSentence[Seq].Satellite1Elevation = IntFromAscii(sentence, commas[4] + 1, commas[5] - commas[4] - 1);
                GSVSentence[Seq].Satellite1Azimuth = IntFromAscii(sentence, commas[5] + 1, commas[6] - commas[5] - 1);
                GSVSentence[Seq].Satellite1SNR = (Byte)IntFromAscii(sentence, commas[6] + 1, commas[7] - commas[6] - 1);

                GSVSentence[Seq].Satellite2Id = IntFromAscii(sentence, commas[7] + 1, commas[8] - commas[7] - 1);
                GSVSentence[Seq].Satellite2Elevation = IntFromAscii(sentence, commas[8] + 1, commas[9] - commas[8] - 1);
                GSVSentence[Seq].Satellite2Azimuth = IntFromAscii(sentence, commas[9] + 1, commas[10] - commas[9] - 1);
                GSVSentence[Seq].Satellite2SNR = (Byte)IntFromAscii(sentence, commas[10] + 1, commas[11] - commas[10] - 1);

                GSVSentence[Seq].Satellite3Id = IntFromAscii(sentence, commas[11] + 1, commas[12] - commas[11] - 1);
                GSVSentence[Seq].Satellite3Elevation = IntFromAscii(sentence, commas[12] + 1, commas[13] - commas[12] - 1);
                GSVSentence[Seq].Satellite3Azimuth = IntFromAscii(sentence, commas[13] + 1, commas[14] - commas[13] - 1);
                GSVSentence[Seq].Satellite3SNR = (Byte)IntFromAscii(sentence, commas[14] + 1, commas[15] - commas[14] - 1);

                GSVSentence[Seq].Satellite4Id = IntFromAscii(sentence, commas[15] + 1, commas[16] - commas[15] - 1);
                GSVSentence[Seq].Satellite4Elevation = IntFromAscii(sentence, commas[16] + 1, commas[17] - commas[16] - 1);
                GSVSentence[Seq].Satellite4Azimuth = IntFromAscii(sentence, commas[17] + 1, commas[18] - commas[17] - 1);
                GSVSentence[Seq].Satellite4SNR = (Byte)IntFromAscii(sentence, commas[18] + 1, sentence.Length - commas[18] - 4);


                b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                GSVSentence[Seq].Checksum = (Byte)((b0 << 4) + b1);
            }
        }

        
        private static void ParseVTG(Byte[] sentence)
        {
            FindCommas(sentence);
            lock (lockVTG)
            {
                VTGSentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                VTGSentence.CourseOverGroundDegrees = DoubleFromAscii(sentence, commas[0] + 1, commas[1] - commas[0] - 1);
                VTGSentence.CourseOverGroundMagnetic = DoubleFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1);
                VTGSentence.SpeedOverGroundKnots = DoubleFromAscii(sentence, commas[4] + 1, commas[5] - commas[4] - 1);
                VTGSentence.SpeedOverGroundKm = DoubleFromAscii(sentence, commas[6] + 1, commas[7] - commas[6] - 1);

                b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                VTGSentence.Checksum = (Byte)((b0 << 4) + b1);
            }
        }


        
        private static void ParseHDT(Byte[] sentence)
        {
            FindCommas(sentence);
            lock (lockHDT)
            {
                HDTSentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                HDTSentence.Heading = DoubleFromAscii(sentence, commas[0] + 1, commas[1] - commas[0] - 1);

                b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                HDTSentence.Checksum = (Byte)((b0 << 4) + b1);
            }
        }

        private static void ParseGLL(Byte[] sentence)
        {
            FindCommas(sentence);
            lock (lockGLL)
            {
                GLLSentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                GLLSentence.FixTime = new DateTime(
                    DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                    IntFromAscii(sentence, commas[4] + 1, 2),
                    IntFromAscii(sentence, commas[4] + 3, 2),
                    IntFromAscii(sentence, commas[4] + 5, 2));
                GLLSentence.Latitude = DoubleFromAscii(sentence, commas[0] + 1, commas[1] - commas[0] - 1) / 100.0f;
                GLLSentence.LatitudeHemisphere = (Char)sentence[commas[1] + 1];
                GLLSentence.Longitude = DoubleFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1) / 100.0f;
                GLLSentence.LongitudePosition = (Char)sentence[commas[3] + 1];
                GLLSentence.Status = (Char)sentence[commas[5] + 1];

                b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                GLLSentence.Checksum = (Byte)((b0 << 4) + b1);
            }
        }
        
        #endregion

        #region Public methods
        public static void Parse(Byte[] NMEASentence)
        {
            // Determine which NMEA sentence has been received
            var index = -1;
            for (var i = 0; i < SupportedPatterns.Length; i++)
            {
                // A supported frame pattern has been found, get its index and exit the loop
                if (NMEASentence[3] == SupportedPatterns[i][0] && NMEASentence[4] == SupportedPatterns[i][1] && NMEASentence[5] == SupportedPatterns[i][2])
                {
                    index = i;
                    break;
                }
            }
            switch (index)
            {
                case 0:     // GGA sentence
                    ParseGGA(NMEASentence);
                    break;
                case 1:     // GSA sentence
                    ParseGSA(NMEASentence);
                    break;
                case 2:     // RMC sentence
                    ParseRMC(NMEASentence);
                    break;
                case 3:     // GSV sentence
                    ParseGSV(NMEASentence);
                    break;
                case 4:     // VTG sentence
                    ParseVTG(NMEASentence);
                    break;
                case 5:     // HDT sentence
                    ParseHDT(NMEASentence);
                    break;
                case 6:     // GLL sentence
                    ParseGLL(NMEASentence);
                    break;
                default:
                    //Debug.WriteLine("Not supported frame received");
                    break;
            }
        }
        #endregion
    }
}
