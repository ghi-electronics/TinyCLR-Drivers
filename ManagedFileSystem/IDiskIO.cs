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


namespace GHIElectronics.TinyCLR.Drivers.ManagedFileSystem {
    public enum DiskControl {
        ControlSync = 0,        /* Complete pending write process (needed at _FS_READONLY == 0) */
        GetSectorCount = 1,     /* Get media size (needed at _USE_MKFS == 1) */
        GetSectorSize = 2,      /* Get sector size (needed at _MAX_SS != _MIN_SS) */
        GetBlockSize = 3,       /* Get erase block size (needed at _USE_MKFS == 1) */
        ControlTrim = 4         /* Inform device that the data on the block of sectors is no longer used (needed at _USE_TRIM == 1) */
    }
    public enum DiskResult {
        Ok = 0,                 /* 0: Successful */
        Error,                  /* 1: R/W Error */
        WriteProtected,         /* 2: Write Protected */
        NotReady,               /* 3: Not Ready */
        InvalidParameter        /* 4: Invalid Parameter */
    }

    public interface IDiskIO {
        byte DiskStatus(byte driveNumber);
        byte DiskInit(byte driveNumber);
        DiskResult DiskRead(byte driveNumber, ref byte[] buffer, uint sector, uint count);
        DiskResult DiskWrite(byte driveNumber, byte[] buffer, uint sector, uint count);
        DiskResult DiskIOControl(byte driveNumber, DiskControl controlCode, ref byte[] buffer);
    }

}
