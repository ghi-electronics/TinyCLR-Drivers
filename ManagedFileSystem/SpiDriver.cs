/*------------------------------------------------------------------------/
/  Foolproof MMCv3/SDv1/SDv2 (in SPI mode) control module
/-------------------------------------------------------------------------/
/
/  Copyright (C) 2013, ChaN, all right reserved.
/
/ * This software is a free software and there is NO WARRANTY.
/ * No restriction on use. You can use, modify and redistribute it for
/   personal, non-profit or commercial products UNDER YOUR RESPONSIBILITY.
/ * Redistributions of source code must retain the above copyright notice.
/
/-------------------------------------------------------------------------/
Features and Limitations:

* Easy to Port Bit-banging SPI
It uses only four GPIO pins. No complex peripheral needs to be used.

* Platform Independent
You need to modify only a few macros to control the GPIO port.

* Low Speed
The data transfer rate will be several times slower than hardware SPI.

* No Media Change Detection
Application program needs to perform a f_mount() after media change.

/-------------------------------------------------------------------------*/

/* Modification by GHI Electronics to support SITCore */
using System;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.ManagedFileSystem {

    internal class SdSpiDriver : IDiskIO {

        SpiDevice spiDev;
        GpioPin chipselectPin;
        public SdSpiDriver(SpiController spiController, GpioPin chipselectPin, uint speed) {
            var settings = new SpiConnectionSettings() {
                ChipSelectType = SpiChipSelectType.None,
                Mode = SpiMode.Mode0,
                ClockFrequency = (int)speed,
            };

            this.spiDev = spiController.GetDevice(settings);
            this.chipselectPin = chipselectPin;
            this.chipselectPin.SetDriveMode(GpioPinDriveMode.Output);
            this.chipselectPin.Write(GpioPinValue.High);

        }


        // SPI transfer
        void TransmitSpi(byte d) {
            byte[] writeBuffer = { d };
            this.spiDev.Write(writeBuffer);
        }

        /* usi.S: Send a 0xFF to the MMC and get the received byte */
        byte ReceiveSpi() {
            byte[] writeBuffer = { 0xff };
            byte[] readBuffer = { 0x00 };

            this.spiDev.TransferFullDuplex(writeBuffer, readBuffer);
            return readBuffer[0];
        }

        /* Disk Status Bits (DSTATUS) */
        const byte StatusNotInitialize = 0x01;   /* Drive not initialized */
        const byte StatusNoDisk = 0x02;   /* No medium in the drive */
        const byte StatusWriteProtected = 0x04;  /* Write protected */

        /* MMC card type flags (MMC_GET_TYPE) */

        const byte CardTypeMMC = 0x01;       /* MMC ver 3 */
        const byte CardTypeSDv1 = 0x02;       /* SD ver 1 */
        const byte CardTypeSDv2 = 0x04;       /* SD ver 2 */
        const byte CardTypeSD = (CardTypeSDv1 | CardTypeSDv2);	/* SD */
        const byte CardTypeBlock = 0x08;     /* Block addressing */

        static void DelayUs(uint n) /* Delay n microseconds (avr-gcc -Os) */
       {
            if (n < 1000) {
                Thread.Sleep(1);
            }
            else {
                Thread.Sleep((int)n / 1000);
            }
        }

        /*--------------------------------------------------------------------------

           Module Private Functions

        ---------------------------------------------------------------------------*/

        /* MMC/SD command (SPI mode) */
        const byte CMD0 = (0);      /* GO_IDLE_STATE */
        const byte CMD1 = (1);      /* SEND_OP_COND */
        const byte ACMD41 = (0x80 + 41);    /* SEND_OP_COND (SDC) */
        const byte CMD8 = (8);      /* SEND_IF_COND */
        const byte CMD9 = (9);      /* SEND_CSD */
        const byte CMD10 = (10);        /* SEND_CID */
        const byte CMD12 = (12);        /* STOP_TRANSMISSION */
        const byte CMD13 = (13);        /* SEND_STATUS */
        const byte ACMD13 = (0x80 + 13);    /* SD_STATUS (SDC) */
        const byte CMD16 = (16);        /* SET_BLOCKLEN */
        const byte CMD17 = (17);        /* READ_SINGLE_BLOCK */
        const byte CMD18 = (18);        /* READ_MULTIPLE_BLOCK */
        const byte CMD23 = (23);        /* SET_BLOCK_COUNT */
        const byte ACMD23 = (0x80 + 23);    /* SET_WR_BLK_ERASE_COUNT (SDC) */
        const byte CMD24 = (24);    /* WRITE_BLOCK */
        const byte CMD25 = (25);    /* WRITE_MULTIPLE_BLOCK */
        const byte CMD32 = (32);    /* ERASE_ER_BLK_START */
        const byte CMD33 = (33);        /* ERASE_ER_BLK_END */
        const byte CMD38 = (38);        /* ERASE */
        const byte CMD55 = (55);        /* APP_CMD */
        const byte CMD58 = (58);		/* READ_OCR */


        byte status = StatusNotInitialize;  /* Disk status */

        byte cardType;          /* b0:MMC, b1:SDv1, b2:SDv2, b3:Block addressing */



        /*-----------------------------------------------------------------------*/
        /* Transmit bytes to the card                                            */
        /*-----------------------------------------------------------------------*/

        void TransmitMMC(
            byte[] buffer,    /* Data to be sent */
            uint bytesToSend		    /* Number of bytes to send */
            ) {
            byte d;
            var bufferIndex = 0;
            do {
                d = buffer[bufferIndex++]; /* Get a byte to be sent */
                this.TransmitSpi(d);
            } while (bufferIndex < bytesToSend);
        }



        /*-----------------------------------------------------------------------*/
        /* Receive bytes from the card                                           */
        /*-----------------------------------------------------------------------*/

        void ReceiveMMC(
            ref byte[] buffer,    /* Pointer to read buffer */
            uint bytesToReceive		        /* Number of bytes to receive */
        ) {
            var rxIndex = 0;
            do {
                buffer[rxIndex++] = this.ReceiveSpi(); /* Store a received byte */
            }
            while (--bytesToReceive > 0);
        }



        /*-----------------------------------------------------------------------*/
        /* Wait for card ready                                                   */
        /*-----------------------------------------------------------------------*/

        int WaitForReady()	/* 1:OK, 0:Timeout */
        {
            var d = new byte[1];
            uint timer;


            for (timer = 5000; timer > 0; timer--) {   /* Wait for ready in timeout of 500ms */
                this.ReceiveMMC(ref d, 1);
                if (d[0] == 0xFF) break;
                DelayUs(100);
            }

            return timer > 1 ? 1 : 0;
        }



        /*-----------------------------------------------------------------------*/
        /* Deselect the card and release SPI bus                                 */
        /*-----------------------------------------------------------------------*/

        void DeselectCard() {
            this.chipselectPin.Write(GpioPinValue.High);
            this.ReceiveSpi();	/* Dummy clock (force DO hi-z for multiple slave SPI) */
        }



        /*-----------------------------------------------------------------------*/
        /* Select the card and wait for ready                                    */
        /*-----------------------------------------------------------------------*/

        int SelectCard()	/* 1:OK, 0:Timeout */
        {
            /* Set CS# low */
            this.chipselectPin.Write(GpioPinValue.Low);
            this.ReceiveSpi();                   /* Dummy clock (force DO enabled) */
            if (this.WaitForReady() > 0) return 1; /* Wait for card ready */

            this.DeselectCard();
            return 0;			            /* Failed */
        }



        /*-----------------------------------------------------------------------*/
        /* Receive a data packet from the card                                   */
        /*-----------------------------------------------------------------------*/

        int ReceiveDataBlock( /* 1:OK, 0:Failed */
            ref byte[] buffer,    /* Data buffer to store received data */
            uint byteCount			/* Byte count */
        ) {
            var d = new byte[2];
            uint timer;


            for (timer = 1000; timer > 0; timer--) {   /* Wait for data packet in timeout of 100ms */
                this.ReceiveMMC(ref d, 1);
                if (d[0] != 0xFF) break;
                DelayUs(100);
            }
            if (d[0] != 0xFE) return 0;     /* If not valid data token, return with error */

            this.ReceiveMMC(ref buffer, byteCount);            /* Receive the data block into buffer */
            this.ReceiveMMC(ref d, 2);                 /* Discard CRC */

            return 1;						/* Return with success */
        }



        /*-----------------------------------------------------------------------*/
        /* Send a data packet to the card                                        */
        /*-----------------------------------------------------------------------*/

        int TransmitDataBlock(   /* 1:OK, 0:Failed */
            byte[] buffer,        /* 512 byte data block to be transmitted */
            byte token			/* Data/Stop token */
        ) {
            var d = new byte[2];


            if (this.WaitForReady() == 0) return 0;

            d[0] = token;
            this.TransmitMMC(d, 1);             /* Xmit a token */
            if (token != 0xFD) {
                /* Is it data token? */
                this.TransmitMMC(buffer, 512);    /* Xmit the 512 byte data block to MMC */
                this.ReceiveMMC(ref d, 2);         /* Xmit dummy CRC (0xFF,0xFF) */
                this.ReceiveMMC(ref d, 1);         /* Receive data response */
                if ((d[0] & 0x1F) != 0x05)  /* If not accepted, return with error */
                    return 0;
            }

            return 1;
        }



        /*-----------------------------------------------------------------------*/
        /* Send a command packet to the card                                     */
        /*-----------------------------------------------------------------------*/

        byte SendCommand(      /* Returns command response (bit7==1:Send failed)*/
            byte command,       /* Command byte */
            uint arg		/* Argument */
        ) {
            byte n;
            var d = new byte[1];
            var buffer = new byte[6];


            if ((command & 0x80) > 0) {
                /* ACMD<n> is the command sequense of CMD55-CMD<n> */
                command &= 0x7F;
                n = this.SendCommand(CMD55, 0);
                if (n > 1) return n;
            }

            /* Select the card and wait for ready except to stop multiple block read */
            if (command != CMD12) {
                this.DeselectCard();
                if (this.SelectCard() == 0) return 0xFF;
            }

            /* Send a command packet */
            buffer[0] = (byte)(0x40 | command);            /* Start + Command index */
            buffer[1] = (byte)(arg >> 24);     /* Argument[31..24] */
            buffer[2] = (byte)(arg >> 16);     /* Argument[23..16] */
            buffer[3] = (byte)(arg >> 8);      /* Argument[15..8] */
            buffer[4] = (byte)arg;             /* Argument[7..0] */
            n = 0x01;                       /* Dummy CRC + Stop */
            if (command == CMD0) n = 0x95;      /* (valid CRC for CMD0(0)) */
            if (command == CMD8) n = 0x87;      /* (valid CRC for CMD8(0x1AA)) */
            buffer[5] = n;
            this.TransmitMMC(buffer, 6);

            /* Receive command response */
            if (command == CMD12) this.ReceiveMMC(ref d, 1);   /* Skip a stuff byte when stop reading */
            n = 10;                                 /* Wait for a valid response in timeout of 10 attempts */
            do
                this.ReceiveMMC(ref d, 1);
            while ((d[0] & 0x80) > 0 && --n > 0);

            return d[0];			/* Return with the response value */
        }



        /*--------------------------------------------------------------------------

           Public Functions

        ---------------------------------------------------------------------------*/


        /*-----------------------------------------------------------------------*/
        /* Get Disk Status                                                       */
        /*-----------------------------------------------------------------------*/

        public byte DiskStatus(
            byte driveNumber			/* Drive number (always 0) */
        ) {
            if (driveNumber > 0) return StatusNotInitialize;

            return this.status;
        }



        /*-----------------------------------------------------------------------*/
        /* Initialize Disk Drive                                                 */
        /*-----------------------------------------------------------------------*/

        public byte DiskInit(
            byte driveNumber		/* Physical drive number (0) */
        ) {
            byte n, ty, cmd;
            var buffer = new byte[4];
            uint timer;
            byte s;


            if (driveNumber > 0) return StatusNotInitialize;

            DelayUs(10000);          /* 10ms */


            for (n = 10; n > 0; n--) this.ReceiveMMC(ref buffer, 1);  /* Apply 80 dummy clocks and the card gets ready to receive command */

            ty = 0;
            if (this.SendCommand(CMD0, 0) == 1) {
                /* Enter Idle state */
                if (this.SendCommand(CMD8, 0x1AA) == 1) {
                    /* SDv2? */
                    this.ReceiveMMC(ref buffer, 4);                           /* Get trailing return value of R7 resp */
                    if (buffer[2] == 0x01 && buffer[3] == 0xAA) {
                        /* The card can work at vdd range of 2.7-3.6V */
                        for (timer = 1000; timer > 0; timer--) {
                            /* Wait for leaving idle state (ACMD41 with HCS bit) */
                            if (this.SendCommand(ACMD41, 0x0001 << 30) == 0) break;
                            DelayUs(1000);
                        }
                        if (timer > 0 && this.SendCommand(CMD58, 0) == 0) {
                            /* Check CCS bit in the OCR */
                            this.ReceiveMMC(ref buffer, 4);
                            ty = ((buffer[0] & 0x40) > 0) ? (byte)(CardTypeSDv2 | CardTypeBlock) : CardTypeSDv2;  /* SDv2 */
                        }
                    }
                }
                else {
                    /* SDv1 or MMCv3 */
                    if (this.SendCommand(ACMD41, 0) <= 1) {
                        ty = CardTypeSDv1; cmd = ACMD41;  /* SDv1 */
                    }
                    else {
                        ty = CardTypeMMC; cmd = CMD1;    /* MMCv3 */
                    }
                    for (timer = 1000; timer > 0; timer--) {
                        /* Wait for leaving idle state */
                        if (this.SendCommand(cmd, 0) == 0) break;
                        DelayUs(1000);
                    }
                    if (timer == 0 || this.SendCommand(CMD16, 512) != 0)  /* Set R/W block length to 512 */
                        ty = 0;
                }
            }
            this.cardType = ty;
            s = (ty > 0) ? (byte)0 : StatusNotInitialize;
            this.status = s;

            this.DeselectCard();

            return s;
        }



        /*-----------------------------------------------------------------------*/
        /* Read Sector(s)                                                        */
        /*-----------------------------------------------------------------------*/

        public DiskResult DiskRead(
            byte driveNumber,           /* Physical drive nmuber (0) */
            ref byte[] buffer,    /* Pointer to the data buffer to store read data */
            uint sector,        /* Start sector number (LBA) */
            uint count			/* Sector count (1..128) */
        ) {
            byte cmd;
            int bufferIndex;
            var workBuffer = new byte[512];

            if ((this.DiskStatus(driveNumber) & StatusNotInitialize) > 0) return DiskResult.NotReady;
            if ((this.cardType & CardTypeBlock) == 0) sector *= 512;  /* Convert LBA to byte address if needed */

            cmd = count > 1 ? CMD18 : CMD17;            /*  READ_MULTIPLE_BLOCK : READ_SINGLE_BLOCK */
            if (this.SendCommand(cmd, sector) == 0) {
                bufferIndex = 0;
                do {
                    if (this.ReceiveDataBlock(ref workBuffer, 512) == 0) break;
                    Array.Copy(workBuffer, 0, buffer, bufferIndex, 512);
                    bufferIndex += 512;
                } while (--count > 0);
                if (cmd == CMD18) this.SendCommand(CMD12, 0);   /* STOP_TRANSMISSION */
            }
            this.DeselectCard();

            return count > 0 ? DiskResult.Error : DiskResult.Ok;
        }



        /*-----------------------------------------------------------------------*/
        /* Write Sector(s)                                                       */
        /*-----------------------------------------------------------------------*/

        public DiskResult DiskWrite(
            byte driveNumber,           /* Physical drive nmuber (0) */
            byte[] buffer,   /* Pointer to the data to be written */
            uint sector,       /* Start sector number (LBA) */
            uint count			/* Sector count (1..128) */
        ) {
            var workBuffer = new byte[12];
            int bufferIndex;

            if ((this.DiskStatus(driveNumber) & StatusNotInitialize) > 0) return DiskResult.NotReady;
            if ((this.cardType & CardTypeBlock) == 0) sector *= 512;  /* Convert LBA to byte address if needed */

            if (count == 1) {
                /* Single block write */
                if ((this.SendCommand(CMD24, sector) == 0)  /* WRITE_BLOCK */
                    && this.TransmitDataBlock(buffer, 0xFE) > 0)
                    count = 0;
            }
            else {
                /* Multiple block write */
                if ((this.cardType & CardTypeSD) > 0) this.SendCommand(ACMD23, count);
                if (this.SendCommand(CMD25, sector) == 0) {
                    /* WRITE_MULTIPLE_BLOCK */
                    bufferIndex = 0;
                    do {
                        Array.Copy(buffer, bufferIndex, workBuffer, 0, 512);
                        if (this.TransmitDataBlock(workBuffer, 0xFC) == 0) break;
                        bufferIndex += 512;
                    } while (--count > 0);
                    if (this.TransmitDataBlock(null, 0xFD) == 0)   /* STOP_TRAN token */
                        count = 1;
                }
            }
            this.DeselectCard();

            return count > 0 ? DiskResult.Error : DiskResult.Ok;
        }


        /*-----------------------------------------------------------------------*/
        /* Miscellaneous Functions                                               */
        /*-----------------------------------------------------------------------*/

        public DiskResult DiskIOControl(
            byte driveNumber,       /* Physical drive nmuber (0) */
            DiskControl controlCode,      /* Control code */
            ref byte[] buffer	/* Buffer to send/receive control data */
        ) {
            DiskResult res;
            byte n;
            var csd = new byte[16];
            uint cs;


            if ((this.DiskStatus(driveNumber) & StatusNotInitialize) > 0) return DiskResult.NotReady;   /* Check if card is in the socket */

            res = DiskResult.Error;
            switch (controlCode) {
                case DiskControl.ControlSync:     /* Make sure that no pending write process */
                    if (this.SelectCard() > 0) res = DiskResult.Ok;
                    break;

                case DiskControl.GetSectorCount:  /* Get number of sectors on the disk (DWORD) */
                    if ((this.SendCommand(CMD9, 0) == 0) && this.ReceiveDataBlock(ref csd, 16) > 0) {
                        if ((csd[0] >> 6) == 1) {   /* SDC ver 2.00 */
                            cs = csd[9] + ((uint)csd[8] << 8) + ((uint)(csd[7] & 63) << 16) + 1;
                            var numberOfSectors = cs << 10;
                            buffer[0] = (byte)(numberOfSectors >> 24);
                            buffer[1] = (byte)(numberOfSectors >> 16);
                            buffer[2] = (byte)(numberOfSectors >> 8);
                            buffer[3] = (byte)numberOfSectors;
                        }
                        else {
                            /* SDC ver 1.XX or MMC */
                            n = (byte)((csd[5] & 15) + ((csd[10] & 128) >> 7) + ((csd[9] & 3) << 1) + 2);
                            cs = (uint)((csd[8] >> 6) + ((uint)csd[7] << 2) + ((uint)(csd[6] & 3) << 10) + 1);
                            var numberOfSectors = cs << (n - 9);
                            buffer[0] = (byte)(numberOfSectors >> 24);
                            buffer[1] = (byte)(numberOfSectors >> 16);
                            buffer[2] = (byte)(numberOfSectors >> 8);
                            buffer[3] = (byte)numberOfSectors;
                        }
                        res = DiskResult.Ok;
                    }
                    break;

                case DiskControl.GetBlockSize:    /* Get erase block size in unit of sector (DWORD) */
                    buffer[0] = 128;
                    res = DiskResult.Ok;
                    break;
                default:
                    res = DiskResult.InvalidParameter;
                    break;
            }

            this.DeselectCard();

            return res;
        }

    }
}
