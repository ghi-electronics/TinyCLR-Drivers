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
        /// <summary>
        /// List of well-known talkers.
        /// <para>"Multiple" is the "GN" talker, which means that data in the sentence comes from different talkers.</para>
        /// </summary>
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

        /// <summary>
        /// Status of parsed sentences
        /// </summary>
        public enum DataStatus
        {
            Valid,
            Invalid,
            InvalidChecksum
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
            public DataStatus DataStatus;
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
            public DataStatus DataStatus;
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
            public DataStatus DataStatus;
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
            public DataStatus DataStatus;
        }

        public struct VTG
        {
            public Int16 TalkerID;
            public Double CourseOverGroundDegrees;
            public Double CourseOverGroundMagnetic;
            public Double SpeedOverGroundKnots;
            public Double SpeedOverGroundKm;
            public Byte Checksum;
            public DataStatus DataStatus;
        }

        public struct HDT
        {
            public Int16 TalkerID;
            public Double Heading;
            public Byte Checksum;
            public DataStatus DataStatus;
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
            public DataStatus DataStatus;
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
            GSVSentence = new GSV[4];
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
        private static Boolean CRLFAppended;

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
            for (var i = 0; i < 20; i++)
            {
                commas[i] = 0;
            }
            for (Byte i = 0, count = 0; i < sentence.Length; i++)
            {
                if (sentence[i] == 44)
                {
                    commas[count++] = i;
                }
            }
        }
        #endregion

        #region private NMEA parsing methods
        private static void ParseRMC(Byte[] sentence)
        {
            ClearRMC();

            lock (lockRMC)
            {
                if (sentence[commas[1] + 1] != 65 && sentence[commas[1] + 1] != 86)
                    return;
                try
                {
                    RMCSentence.DataStatus = DataStatus.Valid;
                    RMCSentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                    RMCSentence.FixTime = new DateTime(
                        IntFromAscii(sentence, commas[8] + 5, 2) + 2000,
                        IntFromAscii(sentence, commas[8] + 3, 2),
                        IntFromAscii(sentence, commas[8] + 1, 2),
                        IntFromAscii(sentence, 7, 2),
                        IntFromAscii(sentence, 9, 2),
                        IntFromAscii(sentence, 11, 2));
                    RMCSentence.Status = (Char)sentence[commas[1] + 1];
                    RMCSentence.Latitude = DoubleFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1) / 100.0f;
                    RMCSentence.LatitudeHemisphere = (Char)sentence[commas[3] + 1];
                    RMCSentence.Longitude = DoubleFromAscii(sentence, commas[4] + 1, commas[5] - commas[4] - 1) / 100.0f;
                    RMCSentence.LongitudePosition = (Char)sentence[commas[5] + 1];
                    RMCSentence.SpeedKnots = DoubleFromAscii(sentence, commas[6] + 1, commas[7] - commas[6] - 1);
                    RMCSentence.SpeedKm = RMCSentence.SpeedKnots * 1.852f;
                    RMCSentence.TrackAngle = DoubleFromAscii(sentence, commas[7] + 1, commas[8] - commas[7] - 1);
                    RMCSentence.MagneticVariation = DoubleFromAscii(sentence, commas[9] + 1, commas[10] - commas[9] - 1);
                    RMCSentence.MagneticVariationDirection = (Char)sentence[commas[10] + 1];

                    if (CRLFAppended)
                    {
                        b0 = (Byte)(sentence[sentence.Length - 4] >= 65 ? sentence[sentence.Length - 4] - 55 : sentence[sentence.Length - 4] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 3] >= 65 ? sentence[sentence.Length - 3] - 55 : sentence[sentence.Length - 3] - 48);
                    }
                    else
                    {
                        b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                    }
                    RMCSentence.Checksum = (Byte)((b0 << 4) + b1);
                    if (!VerifyChecksum(sentence, RMCSentence.Checksum))
                        RMCSentence.DataStatus = DataStatus.InvalidChecksum;
                }
                catch
                {
                    RMCSentence.DataStatus = DataStatus.Invalid;
                }
            }
        }

        private static void ParseGGA(Byte[] sentence)
        {
            ClearGGA();

            lock (lockGGA)
            {
                try
                {
                    GGASentence.DataStatus = DataStatus.Valid;
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
                    GGASentence.QualityIndicator = (Byte)(sentence[commas[5] + 1] - 48);
                    GGASentence.SatellitesInUse = (Byte)IntFromAscii(sentence, commas[6] + 1, commas[7] - commas[6] - 1);
                    GGASentence.HorizontalDilution = DoubleFromAscii(sentence, commas[7] + 1, commas[8] - commas[7] - 1);
                    GGASentence.AntennaAltitude = DoubleFromAscii(sentence, commas[8] + 1, commas[9] - commas[8] - 1);
                    GGASentence.AntennaAltitudeUnit = (Char)sentence[commas[9] + 1];
                    GGASentence.GeoidalSeparation = DoubleFromAscii(sentence, commas[10] + 1, commas[11] - commas[10] - 1);
                    GGASentence.GeoidalSeparationUnit = (Char)sentence[commas[11] + 1];
                    GGASentence.AgeOfDifferentialData = DoubleFromAscii(sentence, commas[12] + 1, commas[13] - commas[12] - 1);
                    GGASentence.DifferentialReferenceStationID = sentence[commas[13] + 1] >= 48 ? IntFromAscii(sentence, commas[13] + 1, 4) : 0;         // Irrelevant if AgeOfDifferentialData = 0.0

                    if (CRLFAppended)
                    {
                        b0 = (Byte)(sentence[sentence.Length - 4] >= 65 ? sentence[sentence.Length - 4] - 55 : sentence[sentence.Length - 4] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 3] >= 65 ? sentence[sentence.Length - 3] - 55 : sentence[sentence.Length - 3] - 48);
                    }
                    else
                    {
                        b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                    }
                    GGASentence.Checksum = (Byte)((b0 << 4) + b1);
                    if (!VerifyChecksum(sentence, GGASentence.Checksum))
                        GGASentence.DataStatus = DataStatus.InvalidChecksum;
                }
                catch
                {
                    GGASentence.DataStatus = DataStatus.Invalid;
                }
            }
        }

        private static void ParseGSA(Byte[] sentence)
        {
            ClearGSA();

            lock (lockGSA)
            {
                try
                {
                    GSASentence.DataStatus = DataStatus.Valid;
                    GSASentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                    GSASentence.SelectionMode = (Char)sentence[commas[0] + 1];
                    GSASentence.Mode = (Byte)(sentence[commas[1] + 1] - 48);
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

                    if (CRLFAppended)
                    {
                        GSASentence.VDOP = DoubleFromAscii(sentence, commas[16] + 1, sentence.Length - commas[16] - 6);
                        b0 = (Byte)(sentence[sentence.Length - 4] >= 65 ? sentence[sentence.Length - 4] - 55 : sentence[sentence.Length - 4] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 3] >= 65 ? sentence[sentence.Length - 3] - 55 : sentence[sentence.Length - 3] - 48);
                    }
                    else
                    {
                        GSASentence.VDOP = DoubleFromAscii(sentence, commas[16] + 1, sentence.Length - commas[16] - 4);
                        b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                    }
                    GSASentence.Checksum = (Byte)((b0 << 4) + b1);
                    if (!VerifyChecksum(sentence, GSASentence.Checksum))
                        GSASentence.DataStatus = DataStatus.InvalidChecksum;
                }
                catch
                {
                    GSASentence.DataStatus = DataStatus.Invalid;
                }
            }
        }

        private static void ParseGSV(Byte[] sentence)
        {
            ClearGSV();

            Int32 numberOfSatellitesToProcess;
            var Seq = sentence[9] - 49;

            lock (lockGSV)
            {
                // No satellite in view
                if (IntFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1) == 0)
                {
                    return;
                }
                
                try
                {
                    GSVSentence[Seq].DataStatus = DataStatus.Valid;
                    GSVSentence[Seq].TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                    GSVSentence[Seq].NumberOfSentences = (Byte)(sentence[7] - 48);
                    GSVSentence[Seq].SequenceNumber = (Byte)(Seq + 1);
                    GSVSentence[Seq].SatellitesInView = (Byte)IntFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1);

                    numberOfSatellitesToProcess = GSVSentence[Seq].SequenceNumber == GSVSentence[Seq].NumberOfSentences ? GSVSentence[Seq].SatellitesInView % 4 : 4;

                    GSVSentence[Seq].Satellite1Id = IntFromAscii(sentence, commas[3] + 1, commas[4] - commas[3] - 1);
                    GSVSentence[Seq].Satellite1Elevation = IntFromAscii(sentence, commas[4] + 1, commas[5] - commas[4] - 1);
                    GSVSentence[Seq].Satellite1Azimuth = IntFromAscii(sentence, commas[5] + 1, commas[6] - commas[5] - 1);
                    GSVSentence[Seq].Satellite1SNR = (Byte)(sentence[commas[6] + 1] >=48 ? (Byte)IntFromAscii(sentence, commas[6] + 1, 2) : 0);

                    if (numberOfSatellitesToProcess >= 2)
                    {
                        GSVSentence[Seq].Satellite2Id = IntFromAscii(sentence, commas[7] + 1, commas[8] - commas[7] - 1);
                        GSVSentence[Seq].Satellite2Elevation = IntFromAscii(sentence, commas[8] + 1, commas[9] - commas[8] - 1);
                        GSVSentence[Seq].Satellite2Azimuth = IntFromAscii(sentence, commas[9] + 1, commas[10] - commas[9] - 1);
                        GSVSentence[Seq].Satellite2SNR = (Byte)(sentence[commas[10] + 1] >= 48 ? (Byte)IntFromAscii(sentence, commas[10] + 1, 2) : 0);
                    }

                    if (numberOfSatellitesToProcess >= 3)
                    {
                        GSVSentence[Seq].Satellite3Id = IntFromAscii(sentence, commas[11] + 1, commas[12] - commas[11] - 1);
                        GSVSentence[Seq].Satellite3Elevation = IntFromAscii(sentence, commas[12] + 1, commas[13] - commas[12] - 1);
                        GSVSentence[Seq].Satellite3Azimuth = IntFromAscii(sentence, commas[13] + 1, commas[14] - commas[13] - 1);
                        GSVSentence[Seq].Satellite3SNR = (Byte)(sentence[commas[14] + 1] >= 48 ? (Byte)IntFromAscii(sentence, commas[14] + 1, 2) : 0);
                    }

                    if (numberOfSatellitesToProcess >= 4)
                    {
                        GSVSentence[Seq].Satellite4Id = IntFromAscii(sentence, commas[15] + 1, commas[16] - commas[15] - 1);
                        GSVSentence[Seq].Satellite4Elevation = IntFromAscii(sentence, commas[16] + 1, commas[17] - commas[16] - 1);
                        GSVSentence[Seq].Satellite4Azimuth = IntFromAscii(sentence, commas[17] + 1, commas[18] - commas[17] - 1);
                        GSVSentence[Seq].Satellite4SNR = (Byte)(sentence[commas[18] + 1] >= 48 ? (Byte)IntFromAscii(sentence, commas[18] + 1, 2) : 0);
                    }

                    if (CRLFAppended)
                    {
                        b0 = (Byte)(sentence[sentence.Length - 4] >= 65 ? sentence[sentence.Length - 4] - 55 : sentence[sentence.Length - 4] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 3] >= 65 ? sentence[sentence.Length - 3] - 55 : sentence[sentence.Length - 3] - 48);
                    }
                    else
                    {
                        b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                    }
                    GSVSentence[Seq].Checksum = (Byte)((b0 << 4) + b1);

                    if (!VerifyChecksum(sentence, GSVSentence[Seq].Checksum))
                        GSVSentence[Seq].DataStatus = DataStatus.InvalidChecksum;
                }
                catch
                {
                    GSVSentence[Seq].DataStatus = DataStatus.Invalid;
                }
            }
        }

        private static void ParseVTG(Byte[] sentence)
        {
            ClearVTG();

            lock (lockVTG)
            {
                try
                {
                    VTGSentence.DataStatus = DataStatus.Valid;
                    VTGSentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                    VTGSentence.CourseOverGroundDegrees = DoubleFromAscii(sentence, commas[0] + 1, commas[1] - commas[0] - 1);
                    VTGSentence.CourseOverGroundMagnetic = DoubleFromAscii(sentence, commas[2] + 1, commas[3] - commas[2] - 1);
                    VTGSentence.SpeedOverGroundKnots = DoubleFromAscii(sentence, commas[4] + 1, commas[5] - commas[4] - 1);
                    VTGSentence.SpeedOverGroundKm = DoubleFromAscii(sentence, commas[6] + 1, commas[7] - commas[6] - 1);

                    if (CRLFAppended)
                    {
                        b0 = (Byte)(sentence[sentence.Length - 4] >= 65 ? sentence[sentence.Length - 4] - 55 : sentence[sentence.Length - 4] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 3] >= 65 ? sentence[sentence.Length - 3] - 55 : sentence[sentence.Length - 3] - 48);
                    }
                    else
                    {
                        b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                    }
                    VTGSentence.Checksum = (Byte)((b0 << 4) + b1);
                    if (!VerifyChecksum(sentence, VTGSentence.Checksum))
                        VTGSentence.DataStatus = DataStatus.InvalidChecksum;
                }
                catch
                {
                    VTGSentence.DataStatus = DataStatus.Invalid;
                }
            }
        }

        private static void ParseHDT(Byte[] sentence)
        {
            ClearHDT();

            lock (lockHDT)
            {
                try
                {
                    HDTSentence.DataStatus = DataStatus.Valid;
                    HDTSentence.TalkerID = (Int16)((sentence[1] << 8) + sentence[2]);
                    HDTSentence.Heading = DoubleFromAscii(sentence, commas[0] + 1, commas[1] - commas[0] - 1);

                    if (CRLFAppended)
                    {
                        b0 = (Byte)(sentence[sentence.Length - 4] >= 65 ? sentence[sentence.Length - 4] - 55 : sentence[sentence.Length - 4] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 3] >= 65 ? sentence[sentence.Length - 3] - 55 : sentence[sentence.Length - 3] - 48);
                    }
                    else
                    {
                        b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                    }
                    HDTSentence.Checksum = (Byte)((b0 << 4) + b1);
                    if (!VerifyChecksum(sentence, HDTSentence.Checksum))
                        HDTSentence.DataStatus = DataStatus.InvalidChecksum;
                }
                catch
                {
                    HDTSentence.DataStatus = DataStatus.Invalid;
                }
            }
        }

        private static void ParseGLL(Byte[] sentence)
        {
            ClearGLL();

            lock (lockGLL)
            {
                try
                {
                    GLLSentence.DataStatus = DataStatus.Valid;
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

                    if (CRLFAppended)
                    {
                        b0 = (Byte)(sentence[sentence.Length - 4] >= 65 ? sentence[sentence.Length - 4] - 55 : sentence[sentence.Length - 4] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 3] >= 65 ? sentence[sentence.Length - 3] - 55 : sentence[sentence.Length - 3] - 48);
                    }
                    else
                    {
                        b0 = (Byte)(sentence[sentence.Length - 2] >= 65 ? sentence[sentence.Length - 2] - 55 : sentence[sentence.Length - 2] - 48);
                        b1 = (Byte)(sentence[sentence.Length - 1] >= 65 ? sentence[sentence.Length - 1] - 55 : sentence[sentence.Length - 1] - 48);
                    }
                    GLLSentence.Checksum = (Byte)((b0 << 4) + b1);
                    if (!VerifyChecksum(sentence, GLLSentence.Checksum))
                        GLLSentence.DataStatus = DataStatus.InvalidChecksum;
                }
                catch
                {
                    GLLSentence.DataStatus = DataStatus.Invalid;
                }
            }
        }

        #endregion

        #region Clear internal sentences
        private static void ClearRMC()
        {
            lock (lockRMC)
            {
                RMCSentence.DataStatus = DataStatus.Invalid;
                RMCSentence.TalkerID = 0;
                RMCSentence.FixTime = new DateTime();
                RMCSentence.Status = Char.MinValue;
                RMCSentence.Latitude = 0.0f;
                RMCSentence.LatitudeHemisphere = Char.MinValue;
                RMCSentence.Longitude = 0.0f;
                RMCSentence.LongitudePosition = Char.MinValue;
                RMCSentence.SpeedKnots = 0.0f;
                RMCSentence.SpeedKm = 0.0f;
                RMCSentence.TrackAngle = 0.0f;
                RMCSentence.MagneticVariation = 0.0f;
                RMCSentence.MagneticVariationDirection = Char.MinValue;
                RMCSentence.Checksum = 0;
            }
        }
        private static void ClearGSV()
        {
            lock (lockGSV)
            {
                for (var i = 0; i < 4; i++)
                {
                    GSVSentence[i].DataStatus = DataStatus.Invalid;

                    GSVSentence[i].TalkerID = 0;
                    GSVSentence[i].NumberOfSentences = 0;
                    GSVSentence[i].SequenceNumber = 0;
                    GSVSentence[i].SatellitesInView = 0;

                    GSVSentence[i].Satellite1Id = 0;
                    GSVSentence[i].Satellite1Elevation = 0;
                    GSVSentence[i].Satellite1Azimuth = 0;
                    GSVSentence[i].Satellite1SNR = 0;

                    GSVSentence[i].Satellite2Id = 0;
                    GSVSentence[i].Satellite2Elevation = 0;
                    GSVSentence[i].Satellite2Azimuth = 0;
                    GSVSentence[i].Satellite2SNR = 0;

                    GSVSentence[i].Satellite3Id = 0;
                    GSVSentence[i].Satellite3Elevation = 0;
                    GSVSentence[i].Satellite3Azimuth = 0;
                    GSVSentence[i].Satellite3SNR = 0;

                    GSVSentence[i].Satellite4Id = 0;
                    GSVSentence[i].Satellite4Elevation = 0;
                    GSVSentence[i].Satellite4Azimuth = 0;
                    GSVSentence[i].Satellite4SNR = 0;

                    GSVSentence[i].Checksum = 0;
                }
            }
        }

        private static void ClearGGA()
        {
            lock (lockGGA)
            {
                GGASentence.DataStatus = DataStatus.Invalid;
                GGASentence.TalkerID = 0;
                GGASentence.FixTime = new DateTime();
                GGASentence.Latitude = 0.0f;
                GGASentence.LatitudeHemisphere = Char.MinValue;
                GGASentence.Longitude = 0.0f;
                GGASentence.LongitudePosition = Char.MinValue;
                GGASentence.QualityIndicator = 0;
                GGASentence.SatellitesInUse = 0;
                GGASentence.HorizontalDilution = 0.0f;
                GGASentence.AntennaAltitude = 0.0f;
                GGASentence.AntennaAltitudeUnit = Char.MinValue;
                GGASentence.GeoidalSeparation = 0.0f;
                GGASentence.GeoidalSeparationUnit = Char.MinValue;
                GGASentence.AgeOfDifferentialData = 0.0f;
                GGASentence.DifferentialReferenceStationID = 0;
                GGASentence.Checksum = 0;
            }
        }

        private static void ClearGSA()
        {
            lock (lockGSA)
            {
                GSASentence.DataStatus = DataStatus.Invalid;
                GSASentence.TalkerID = 0;
                GSASentence.SelectionMode = Char.MinValue;
                GSASentence.Mode = 0;
                GSASentence.Satellite1Id = 0;
                GSASentence.Satellite2Id = 0;
                GSASentence.Satellite3Id = 0;
                GSASentence.Satellite4Id = 0;
                GSASentence.Satellite5Id = 0;
                GSASentence.Satellite6Id = 0;
                GSASentence.Satellite7Id = 0;
                GSASentence.Satellite8Id = 0;
                GSASentence.Satellite9Id = 0;
                GSASentence.Satellite10Id = 0;
                GSASentence.Satellite11Id = 0;
                GSASentence.Satellite12Id = 0;
                GSASentence.PDOP = 0.0f;
                GSASentence.HDOP = 0.0f;
                GSASentence.VDOP = 0.0f;
                GSASentence.Checksum = 0;
            }
        }

        private static void ClearVTG()
        {
            lock (lockVTG)
            {
                VTGSentence.DataStatus = DataStatus.Invalid;
                VTGSentence.TalkerID = 0;
                VTGSentence.CourseOverGroundDegrees = 0.0f;
                VTGSentence.CourseOverGroundMagnetic = 0.0f;
                VTGSentence.SpeedOverGroundKnots = 0.0f;
                VTGSentence.SpeedOverGroundKm = 0.0f;
                VTGSentence.Checksum = 0;
            }
        }

        private static void ClearHDT()
        {
            lock (lockHDT)
            {
                HDTSentence.DataStatus = DataStatus.Invalid;
                HDTSentence.TalkerID = 0;
                HDTSentence.Heading = 0.0f;
                HDTSentence.Checksum = 0;
            }
        }

        private static void ClearGLL()
        {
            lock (lockGLL)
            {
                GLLSentence.DataStatus = DataStatus.Invalid;
                GLLSentence.TalkerID = 0;
                GLLSentence.FixTime = new DateTime();
                GLLSentence.Latitude = 0.0f;
                GLLSentence.LatitudeHemisphere = Char.MinValue;
                GLLSentence.Longitude = 0.0f;
                GLLSentence.LongitudePosition = Char.MinValue;
                GLLSentence.Status = Char.MinValue;
                GLLSentence.Checksum = 0;
            }
        }
        
        #endregion

        #region Public methods
        /// <summary>
        /// Parse the NMEA sentence if found in the supported patterns
        /// </summary>
        /// <param name="NMEASentence">A byte array containing the NMEA sentence</param>
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
                    FindCommas(NMEASentence);

                    break;
                }
            }

            // Some MikroElektronika GPs do not send CRLF at the end of the sentence (GNSS Zoe, so far) so we have to check this
            CRLFAppended = NMEASentence[NMEASentence.Length - 2] == 13 && NMEASentence[NMEASentence.Length - 1] == 10;

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

        /// <summary>Verifies the checksum of a given NMEA sentence</summary>
        /// <param name="frame">  The complete sentence, including starting '$' and trailing '*' chars.</param>
        /// <param name="checksum">The checksum to check</param>
        /// <returns>
        /// True if both checksums match, False otherwise.
        /// </returns>
        public static Boolean VerifyChecksum(Byte[] sentence, Byte checksum)
        {
            Int32 calculatedChecksum = sentence[1];
            var sentenceLength = CRLFAppended ? sentence.Length - 2 : sentence.Length;
            for (var i = 2; i < sentenceLength - 3; i++)
            {
                calculatedChecksum ^= sentence[i];
            }
            return checksum == calculatedChecksum;
        }

        /// <summary>Calculates the checksum of an NMEA sentence.</summary>
        /// <param name="sentence">The byte array representing the NMEA sentence, excluding the starting $ and the ending * chars.</param>
        /// <returns>The calculated checksum</returns>
        public static Byte CalculateChecksum(Byte[] sentence)
        {
            Int32 checksum = sentence[0];
            for (var i = 1; i < sentence.Length; i++)
            {
                checksum ^= sentence[i];
            }
            return (Byte)checksum;
        }
        #endregion
    }
}
