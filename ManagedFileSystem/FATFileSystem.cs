/*----------------------------------------------------------------------------/
/  FatFs - Generic FAT Filesystem Module  R0.13b                              /
/-----------------------------------------------------------------------------/
/
/ Copyright (C) 2018, ChaN, all right reserved.
/
/ FatFs module is an open source software. Redistribution and use of FatFs in
/ source and binary forms, with or without modification, are permitted provided
/ that the following condition is met:
/
/ 1. Redistributions of source code must retain the above copyright notice,
/    this condition and the following disclaimer.
/
/ This software is provided by the copyright holder and contributors "AS IS"
/ and any warranties related to this software are DISCLAIMED.
/ The copyright owner or contributors be NOT LIABLE for any damages caused
/ by use of this software.
/
/----------------------------------------------------------------------------*/

/* Modification by GHI Electronics to support SITCore */

using System;
using System.Text;

namespace GHIElectronics.TinyCLR.Drivers.ManagedFileSystem {

    public class FATFileSystem {

        IDiskIO diskIO;
        public FATFileSystem(IDiskIO io) => this.diskIO = io;



        #region High level defines

        const int FF_VOLUMES = 1;
        const int FF_FS_EXFAT = 0;
        const int FF_FS_RPATH = 0;
        #endregion

        #region  Original C include file definition

        public const int FF_MAX_SS = 512;

        public class FatFS {

            public byte fs_type;       /* Filesystem type (0:N/A) */
            public byte pdrv;          /* Physical drive number */
            public byte n_fats;        /* Number of FATs (1 or 2) */
            public byte wflag;         /* win[] flag (b0:dirty) */
            public byte fsi_flag;      /* FSINFO flags (b7:disabled, b0:dirty) */
            public byte id;            /* Volume mount ID */
            public uint n_rootdir;     /* Number of root directory entries (FAT12/16) */
            public uint csize;         /* Cluster size [sectors] */
            public uint last_clst;     /* Last allocated cluster */
            public uint free_clst;     /* Number of free clusters */
            public uint n_fatent;      /* Number of FAT entries (number of clusters + 2) */
            public uint fsize;         /* Size of an FAT [sectors] */
            public uint volbase;       /* Volume base sector */
            public uint fatbase;       /* FAT base sector */
            public uint dirbase;       /* Root directory base sector/cluster */
            public uint database;      /* Data base sector */
            public uint winsect;       /* Current sector appearing in the win[] */
            public byte[] win;         /* Disk access window for Directory, FAT (and file data at tiny cfg) */


            public FatFS() => this.win = new byte[FF_MAX_SS];
        }

        /* Object ID and allocation information (FFOBJID) */

        public class FileObjectIdentifier {
            public FatFS fs;           /* Pointer to the hosting volume of this object */
            public uint id;            /* Hosting volume mount ID */
            public byte attr;          /* Object attribute */
            public byte stat;          /* Object chain status (b1-0: =0:not contiguous, =2:contiguous, =3:flagmented in this session, b2:sub-directory stretched) */
            public uint sclust;        /* Object data start cluster (0:no cluster or root directory) */
            public uint objsize;       /* Object size (valid when sclust != 0) */

            public FileObjectIdentifier() => this.fs = new FatFS();

            internal FileObjectIdentifier Clone(FatFS fs) {
                var clone = (FileObjectIdentifier)this.MemberwiseClone();
                clone.fs = fs;
                return clone;
            }
        }

        /* File object structure (FIL) */

        public class FileObject {
            public FileObjectIdentifier obj;                 /* Object identifier (must be the 1st member to detect invalid object pointer) */
            public byte flag;                   /* File status flags */
            public byte err;                    /* Abort flag (error code) */
            public uint fptr;                   /* File read/write pointer (Zeroed on file open) */
            public uint clust;                  /* Current cluster of fpter (invalid when fptr is 0) */
            public uint sect;                   /* Sector number appearing in buf[] (0:invalid) */
            public byte[] buf;                  /* File private data read/write window */
            public uint dir_sect;               /* Sector number containing the directory entry (not used at exFAT) */
            public uint dir_ptrAsFsWinOffset;	/* Pointer to the directory entry in the win[] (not used at exFAT) */

            public FileObject() {
                this.obj = new FileObjectIdentifier();
                this.buf = new byte[FF_MAX_SS];
            }
        }

        /* Directory object structure (DIR) */

        public class DirectoryObject {
            public FileObjectIdentifier obj;                 /* Object identifier */
            public uint dptr;                   /* Current read/write offset */
            public uint clust;                  /* Current cluster */
            public uint sect;                   /* Current sector (0:Read operation has terminated) */
            public uint dirAsFsWinOffset;	    // Changed: Offset from Fs.win to directory item       /* Pointer to the directory item in the win[] */
            public byte[] fn;                   /* SFN (in/out) {body[8],ext[3],status[1]} */

            public DirectoryObject() {
                this.fn = new byte[12];
                this.obj = new FileObjectIdentifier();
            }

            internal DirectoryObject Clone(FatFS fs) {
                var clone = new DirectoryObject {
                    obj = this.obj.Clone(fs),
                    dptr = this.dptr,
                    clust = this.clust,
                    sect = this.sect,
                    dirAsFsWinOffset = this.dirAsFsWinOffset,
                };
                Array.Copy(clone.fn, this.fn, this.fn.Length);
                return clone;
            }
        }


        /* File information structure (FILINFO) */

        public class FileInfo {

            public uint fileSize;     /* File size */
            public uint fileDate;     /* Modified date */
            public uint fileTime;     /* Modified time */
            public byte fileAttribute;   /* File attribute */
            public byte[] fileName;   /* File name */

            public FileInfo() => this.fileName = new byte[12 + 1];
        }

        /* File function return code (FRESULT) */

        public enum FileResult {
            Ok = 0,              /* (0) Succeeded */
            DiskError,            /* (1) A hard error occurred in the low level disk I/O layer */
            InternalError,             /* (2) Assertion failed */
            NotReady,           /* (3) The physical drive cannot work */
            FileNotExist,             /* (4) Could not find the file */
            PathNotFound,             /* (5) Could not find the path */
            InvalidPathName,        /* (6) The path name format is invalid */
            AccessDenied,              /* (7) Access denied due to prohibited access or directory full */
            Exists,               /* (8) Access denied due to prohibited access */
            InvalidObject,      /* (9) The file/directory object is invalid */
            WriteProtected,     /* (10) The physical drive is write protected */
            InvalidDrive,       /* (11) The logical drive number is invalid */
            NotEnabled,         /* (12) The volume has no work area */
            NoFileSystem,       /* (13) There is no valid FAT volume */
            MKFSAborted,        /* (14) The f_mkfs() aborted due to any problem */
            TimeOut,             /* (15) Could not get a grant to access the volume within defined period */
            Locked,              /* (16) The operation is rejected according to the file sharing policy */
            NotEnoughCore,     /* (17) LFN working buffer could not be allocated */
            TooManyOpenFiles, /* (18) Number of open files > FF_FS_LOCK */
            InvalidParameter,   /* (19) Given parameter is invalid */

        }

        const int EOF = -1;

        /*--------------------------------------------------------------*/
        /* Flags and offset address                                     */


        /* File access mode and open method flags (3rd argument of f_open) */
       


        public const byte FA_READ = 0x01;
        public const byte FA_WRITE = 0x02;
        public const byte FA_OPEN_EXISTING = 0x00;
        public const byte FA_CREATE_NEW = 0x04;
        public const byte FA_CREATE_ALWAYS = 0x08;
        public const byte FA_OPEN_ALWAYS = 0x10;
        public const byte FA_OPEN_APPEND = 0x30;

        /* Fast seek controls (2nd argument of f_lseek) */


        /* Format options (2nd argument of f_mkfs) */
        const byte FM_FAT = 0x01;
        const byte FM_FAT32 = 0x02;
        const byte FM_EXFAT = 0x04;
        const byte FM_ANY = 0x07;
        const byte FM_SFD = 0x08;

        /* Filesystem type (FATFS.fs_type) */
        public const byte FS_FAT12 = 1;
        public const byte FS_FAT16 = 2;
        public const byte FS_FAT32 = 3;
        public const byte FS_EXFAT = 4;

        /* File attribute bits for directory entry (FILINFO.fattrib) */
        public const byte AM_RDO = 0x01;   /* Read only */
        public const byte AM_HID = 0x02;   /* Hidden */
        public const byte AM_SYS = 0x04;   /* System */
        public const byte AM_DIR = 0x10;   /* Directory */
        public const byte AM_ARC = 0x20;   /* Archive */
        #endregion



        /* Character code support macros */
        static bool IsUpper(char c) => c >= 'A' && c <= 'Z';

        static bool IsLower(char c) => c >= 'a' && c <= 'z';

        static bool IsDigit(char c) => c >= '0' && c <= '9';

        static bool IsSurrogate(char c) => c >= 0xD800 && c <= 0xDFFF;

        static bool IsSurrogateH(char c) => c >= 0xD800 && c <= 0xDBFF;

        static bool IsSurrogateL(char c) => c >= 0xDC00 && c <= 0xDFFF;

        /* Additional file attribute bits for internal use */
        const byte AM_VOL = 0x08;   /* Volume label */
        const byte AM_LFN = 0x0F;   /* LFN entry */
        const byte AM_MASK = 0x3F;  /* Mask of defined bits */


        /* Additional file access control and file status flags for internal use */
        const byte FA_SEEKEND = 0x20;   /* Seek to end of the file on file open */
        const byte FA_MODIFIED = 0x40;  /* File has been modified */
        const byte FA_DIRTY = 0x80;     /* FIL.buf[] needs to be written-back */


        /* Name status flags in fn[11] */
        const byte NSFLAG = 11;     /* Index of the name status byte */
        const byte NS_LOSS = 0x01;  /* Out of 8.3 format */
        const byte NS_LFN = 0x02;   /* Force to create LFN entry */
        const byte NS_LAST = 0x04;  /* Last segment */
        const byte NS_BODY = 0x08;  /* Lower case flag (body) */
        const byte NS_EXT = 0x10;   /* Lower case flag (ext) */
        const byte NS_DOT = 0x20;   /* Dot entry */
        const byte NS_NOLFN = 0x40; /* Do not find LFN */
        const byte NS_NONAME = 0x80;/* Not followed */


        /* Limits and boundaries */
        const uint MAX_DIR = 0x200000;      /* Max size of FAT directory */
        const uint MAX_DIR_EX = 0x10000000; /* Max size of exFAT directory */
        const uint MAX_FAT12 = 0xFF5;       /* Max FAT12 clusters (differs from specs, but right for real DOS/Windows behavior) */
        const uint MAX_FAT16 = 0xFFF5;      /* Max FAT16 clusters (differs from specs, but right for real DOS/Windows behavior) */
        const uint MAX_FAT32 = 0x0FFFFFF5;  /* Max FAT32 clusters (not specified, practical limit) */
        const uint MAX_EXFAT = 0x7FFFFFFD;  /* Max exFAT clusters (differs from specs, implementation limit) */


        /* FatFs refers the FAT structure as simple byte array instead of structure member
        / because the C structure is not binary compatible between different platforms */

        const uint BS_JmpBoot = 0;          /* x86 jump instruction (3-byte) */
        const uint BS_OEMName = 3;          /* OEM name (8-byte) */
        const uint BPB_BytsPerSec = 11;     /* Sector size [byte] (WORD) */
        const uint BPB_SecPerClus = 13;     /* Cluster size [sector] (BYTE) */
        const uint BPB_RsvdSecCnt = 14;     /* Size of reserved area [sector] (WORD) */
        const uint BPB_NumFATs = 16;        /* Number of FATs (BYTE) */
        const uint BPB_RootEntCnt = 17;     /* Size of root directory area for FAT [entry] (WORD) */
        const uint BPB_TotSec16 = 19;       /* Volume size (16-bit) [sector] (WORD) */
        const uint BPB_Media = 21;          /* Media descriptor byte (BYTE) */
        const uint BPB_FATSz16 = 22;        /* FAT size (16-bit) [sector] (WORD) */
        const uint BPB_SecPerTrk = 24;      /* Number of sectors per track for int13h [sector] (WORD) */
        const uint BPB_NumHeads = 26;       /* Number of heads for int13h (WORD) */
        const uint BPB_HiddSec = 28;        /* Volume offset from top of the drive (DWORD) */
        const uint BPB_TotSec32 = 32;       /* Volume size (32-bit) [sector] (DWORD) */
        const uint BS_DrvNum = 36;          /* Physical drive number for int13h (BYTE) */
        const uint BS_NTres = 37;           /* WindowsNT error flag (BYTE) */
        const uint BS_BootSig = 38;         /* Extended boot signature (BYTE) */
        const uint BS_VolID = 39;           /* Volume serial number (DWORD) */
        const uint BS_VolLab = 43;          /* Volume label string (8-byte) */
        const uint BS_FilSysType = 54;      /* Filesystem type string (8-byte) */
        const uint BS_BootCode = 62;        /* Boot code (448-byte) */
        const uint BS_55AA = 510;           /* Signature word (WORD) */

        const uint BPB_FATSz32 = 36;        /* FAT32: FAT size [sector] (DWORD) */
        const uint BPB_ExtFlags32 = 40;     /* FAT32: Extended flags (WORD) */
        const uint BPB_FSVer32 = 42;        /* FAT32: Filesystem version (WORD) */
        const uint BPB_RootClus32 = 44;     /* FAT32: Root directory cluster (DWORD) */
        const uint BPB_FSInfo32 = 48;       /* FAT32: Offset of FSINFO sector (WORD) */
        const uint BPB_BkBootSec32 = 50;    /* FAT32: Offset of backup boot sector (WORD) */
        const uint BS_DrvNum32 = 64;        /* FAT32: Physical drive number for int13h (BYTE) */
        const uint BS_NTres32 = 65;         /* FAT32: Error flag (BYTE) */
        const uint BS_BootSig32 = 66;       /* FAT32: Extended boot signature (BYTE) */
        const uint BS_VolID32 = 67;         /* FAT32: Volume serial number (DWORD) */
        const uint BS_VolLab32 = 71;        /* FAT32: Volume label string (8-byte) */
        const uint BS_FilSysType32 = 82;    /* FAT32: Filesystem type string (8-byte) */
        const uint BS_BootCode32 = 90;      /* FAT32: Boot code (420-byte) */

        const uint BPB_ZeroedEx = 11;       /* exFAT: MBZ field (53-byte) */
        const uint BPB_VolOfsEx = 64;       /* exFAT: Volume offset from top of the drive [sector] (QWORD) */
        const uint BPB_TotSecEx = 72;       /* exFAT: Volume size [sector] (QWORD) */
        const uint BPB_FatOfsEx = 80;       /* exFAT: FAT offset from top of the volume [sector] (DWORD) */
        const uint BPB_FatSzEx = 84;        /* exFAT: FAT size [sector] (DWORD) */
        const uint BPB_DataOfsEx = 88;      /* exFAT: Data offset from top of the volume [sector] (DWORD) */
        const uint BPB_NumClusEx = 92;      /* exFAT: Number of clusters (DWORD) */
        const uint BPB_RootClusEx = 96;     /* exFAT: Root directory start cluster (DWORD) */
        const uint BPB_VolIDEx = 100;       /* exFAT: Volume serial number (DWORD) */
        const uint BPB_FSVerEx = 104;       /* exFAT: Filesystem version (WORD) */
        const uint BPB_VolFlagEx = 106;     /* exFAT: Volume flags (WORD) */
        const uint BPB_BytsPerSecEx = 108;  /* exFAT: Log2 of sector size in unit of byte (BYTE) */
        const uint BPB_SecPerClusEx = 109;  /* exFAT: Log2 of cluster size in unit of sector (BYTE) */
        const uint BPB_NumFATsEx = 110;     /* exFAT: Number of FATs (BYTE) */
        const uint BPB_DrvNumEx = 111;      /* exFAT: Physical drive number for int13h (BYTE) */
        const uint BPB_PercInUseEx = 112;   /* exFAT: Percent in use (BYTE) */
        const uint BPB_RsvdEx = 113;        /* exFAT: Reserved (7-byte) */
        const uint BS_BootCodeEx = 120;     /* exFAT: Boot code (390-byte) */

        const uint DIR_Name = 0;            /* Short file name (11-byte) */
        const uint DIR_Attr = 11;           /* Attribute (BYTE) */
        const uint DIR_NTres = 12;          /* Lower case flag (BYTE) */
        const uint DIR_CrtTime10 = 13;      /* Created time sub-second (BYTE) */
        const uint DIR_CrtTime = 14;        /* Created time (DWORD) */
        const uint DIR_LstAccDate = 18;     /* Last accessed date (WORD) */
        const uint DIR_FstClusHI = 20;      /* Higher 16-bit of first cluster (WORD) */
        const uint DIR_ModTime = 22;        /* Modified time (DWORD) */
        const uint DIR_FstClusLO = 26;      /* Lower 16-bit of first cluster (WORD) */
        const uint DIR_FileSize = 28;       /* File size (DWORD) */
        const uint LDIR_Ord = 0;            /* LFN: LFN order and LLE flag (BYTE) */
        const uint LDIR_Attr = 11;          /* LFN: LFN attribute (BYTE) */
        const uint LDIR_Type = 12;          /* LFN: Entry type (BYTE) */
        const uint LDIR_Chksum = 13;        /* LFN: Checksum of the SFN (BYTE) */
        const uint LDIR_FstClusLO = 26;     /* LFN: MBZ field (WORD) */
        const uint XDIR_Type = 0;           /* exFAT: Type of exFAT directory entry (BYTE) */
        const uint XDIR_NumLabel = 1;       /* exFAT: Number of volume label characters (BYTE) */
        const uint XDIR_Label = 2;          /* exFAT: Volume label (11-WORD) */
        const uint XDIR_CaseSum = 4;        /* exFAT: Sum of case conversion table (DWORD) */
        const uint XDIR_NumSec = 1;         /* exFAT: Number of secondary entries (BYTE) */
        const uint XDIR_SetSum = 2;         /* exFAT: Sum of the set of directory entries (WORD) */
        const uint XDIR_Attr = 4;           /* exFAT: File attribute (WORD) */
        const uint XDIR_CrtTime = 8;        /* exFAT: Created time (DWORD) */
        const uint XDIR_ModTime = 12;       /* exFAT: Modified time (DWORD) */
        const uint XDIR_AccTime = 16;       /* exFAT: Last accessed time (DWORD) */
        const uint XDIR_CrtTime10 = 20;     /* exFAT: Created time subsecond (BYTE) */
        const uint XDIR_ModTime10 = 21;     /* exFAT: Modified time subsecond (BYTE) */
        const uint XDIR_CrtTZ = 22;         /* exFAT: Created timezone (BYTE) */
        const uint XDIR_ModTZ = 23;         /* exFAT: Modified timezone (BYTE) */
        const uint XDIR_AccTZ = 24;         /* exFAT: Last accessed timezone (BYTE) */
        const uint XDIR_GenFlags = 33;      /* exFAT: General secondary flags (BYTE) */
        const uint XDIR_NumName = 35;       /* exFAT: Number of file name characters (BYTE) */
        const uint XDIR_NameHash = 36;      /* exFAT: Hash of file name (WORD) */
        const uint XDIR_ValidFileSize = 40; /* exFAT: Valid file size (QWORD) */
        const uint XDIR_FstClus = 52;       /* exFAT: First cluster of the file data (DWORD) */
        const uint XDIR_FileSize = 56;      /* exFAT: File/Directory size (QWORD) */

        const uint SZDIRE = 32;             /* Size of a directory entry */
        const byte DDEM = 0xE5;             /* Deleted directory entry mark set to DIR_Name[0] */
        const byte RDDEM = 0x05;            /* Replacement of the character collides with DDEM */
        const byte LLEF = 0x40;             /* Last long entry flag in LDIR_Ord */

        const uint FSI_LeadSig = 0;         /* FAT32 FSI: Leading signature (DWORD) */
        const uint FSI_StrucSig = 484;      /* FAT32 FSI: Structure signature (DWORD) */
        const uint FSI_Free_Count = 488;    /* FAT32 FSI: Number of free clusters (DWORD) */
        const uint FSI_Nxt_Free = 492;      /* FAT32 FSI: Last allocated cluster (DWORD) */

        const uint MBR_Table = 446;         /* MBR: Offset of partition table in the MBR */
        const uint SZ_PTE = 16;             /* MBR: Size of a partition table entry */
        const uint PTE_Boot = 0;            /* MBR PTE: Boot indicator */
        const uint PTE_StHead = 1;          /* MBR PTE: Start head */
        const uint PTE_StSec = 2;           /* MBR PTE: Start sector */
        const uint PTE_StCyl = 3;           /* MBR PTE: Start cylinder */
        const uint PTE_System = 4;          /* MBR PTE: System ID */
        const uint PTE_EdHead = 5;          /* MBR PTE: End head */
        const uint PTE_EdSec = 6;           /* MBR PTE: End sector */
        const uint PTE_EdCyl = 7;           /* MBR PTE: End cylinder */
        const uint PTE_StLba = 8;           /* MBR PTE: Start in LBA */
        const uint PTE_SizLba = 12;         /* MBR PTE: Size in LBA */

        /*--------------------------------------------------------------------------

           Module Private Work Area

        ---------------------------------------------------------------------------*/
        /* Remark: Variables defined here without initial value shall be guaranteed
        /  zero/null at start-up. If not, the linker option or start-up routine is
        /  not compliance with C standard. */

        /*--------------------------------*/
        /* File/Volume controls           */
        /*--------------------------------*/

        static FatFS[] fatFs = new FatFS[FF_VOLUMES];   /* Pointer to the filesystem objects (logical drives) */
        static byte fsid;					            /* Filesystem mount ID */
        static string[] volumeStr = { "RAM", "NAND", "CF", "SD", "SD2", "USB", "USB2", "USB3" }; /* Pre-defined volume ID */

        /* Disk Status Bits (DSTATUS) */
        const byte STA_NOINIT = 0x01;   /* Drive not initialized */
        const byte STA_NODISK = 0x02;   /* No medium in the drive */
        const byte STA_PROTECT = 0x04;	/* Write protected */

        /*--------------------------------------------------------------------------

           Module Private Functions

        ---------------------------------------------------------------------------*/


        /*-----------------------------------------------------------------------*/
        /* Load/Store multi-byte word in the FAT structure                       */
        /*-----------------------------------------------------------------------*/

        static uint LoadWord(byte[] ptr, uint offset)	/*	 Load a 2-byte little-endian word */
        {

            uint rv;

            rv = ptr[1 + offset];
            rv = rv << 8 | ptr[0 + offset];
            return rv;
        }

        static uint LoadDword(byte[] ptr, uint offset)	/* Load a 4-byte little-endian word */
        {

            uint rv;

            rv = ptr[3 + offset];
            rv = rv << 8 | ptr[2 + offset];
            rv = rv << 8 | ptr[1 + offset];
            rv = rv << 8 | ptr[0 + offset];
            return rv;
        }

        static void StoreWord(ref byte[] ptr, uint offset, uint val)    /* Store a 2-byte word in little-endian */
        {
            ptr[0 + offset] = (byte)val; val >>= 8;
            ptr[1 + offset] = (byte)val;
        }

        static void StoreDword(ref byte[] ptr, uint offset, uint val)  /* Store a 4-byte word in little-endian */
        {
            ptr[0 + offset] = (byte)val; val >>= 8;
            ptr[1 + offset] = (byte)val; val >>= 8;
            ptr[2 + offset] = (byte)val; val >>= 8;
            ptr[3 + offset] = (byte)val;
        }

        /* Copy memory to memory */
        static void CopyMemory(ref byte[] dst, byte[] src, uint count) {
            for (var i = 0; i < count; i++) {
                dst[i] = src[i];
            }
        }

        static void CopyMemory(ref byte[] dst, int dstOffset, byte[] src, uint count) {
            for (var i = 0; i < count; i++) {
                dst[i + dstOffset] = src[i];
            }
        }

        static void CopyMemory(ref byte[] dst, int dstOffset, byte[] src, int srcOffset, uint count) {
            for (var i = 0; i < count; i++) {
                dst[i + dstOffset] = src[i + srcOffset];
            }
        }


        /* Fill memory block */
        static void SetMemory(ref byte[] dst, int val, uint count) {
            for (var i = 0; i < count; i++) {
                dst[i] = (byte)val;
            }
        }

        static void SetMemory(ref byte[] dst, int dstOffset, int val, uint count) {
            for (var i = 0; i < count; i++) {
                dst[i + dstOffset] = (byte)val;
            }
        }

        /* Compare memory to memory */
        static int CompareMemory(byte[] dst, byte[] src, int count) {
            int dIndex = 0, sIndex = 0;
            byte d, s;
            var r = 0;
            do {
                d = dst[dIndex++]; s = src[sIndex++];
                r = d - s;
            }
            while (--count > 0 && (r == 0));
            return r;
        }

        /* Test if the character is DBC 1st byte */
        static int CheckDBCFirstByte(byte c) {
            /* SBCS fixed code page */
            if (c != 0) return 0;	/* Always false */
            return 0;
        }

        /* Test if the character is DBC 2nd byte */
        static int CheckDBCSecondByte(byte c) {
            /* SBCS fixed code page */
            if (c != 0) return 0;	/* Always false */
            return 0;
        }

        const uint FF_FS_NORTC = 1;
        const uint FF_NORTC_MON = 1;
        const uint FF_NORTC_MDAY = 1;
        const uint FF_NORTC_YEAR = 2018;

        /* Check if chr is contained in the string */
        static int ContainsChar(string str, byte chr)	/* NZ:contained, ZR:not contained */
        {
            for (var i = 0; i < str.Length; i++) {
                if ((byte)str[i] == chr) {
                    return 1;
                }
            }
            return 0;
        }

        uint GetFatTime() => (uint)(FF_NORTC_YEAR - 1980) << 25 | (uint)FF_NORTC_MON << 21 | (uint)FF_NORTC_MDAY << 16;

        /*-----------------------------------------------------------------------*/
        /* Move/Flush disk access window in the filesystem object                */
        /*-----------------------------------------------------------------------*/

        FileResult SyncWindow( /* Returns FR_OK or FR_DISK_ERR */
            ref FatFS fs           /* Filesystem object */
        ) {
            var res = FileResult.Ok;


            if (fs.wflag > 0) {   /* Is the disk access window dirty */
                if (this.diskIO.DiskWrite(fs.pdrv, fs.win, fs.winsect, 1) == DiskResult.Ok) {   /* Write back the window */
                    fs.wflag = 0;  /* Clear window dirty flag */
                    if (fs.winsect - fs.fatbase < fs.fsize) {   /* Is it in the 1st FAT? */
                        if (fs.n_fats == 2) this.diskIO.DiskWrite(fs.pdrv, fs.win, fs.winsect + fs.fsize, 1); /* Reflect it to 2nd FAT if needed */
                    }
                }
                else {
                    res = FileResult.DiskError;
                }
            }
            return res;
        }


        FileResult MoveWindow( /* Returns FR_OK or FR_DISK_ERR */
            ref FatFS fs,          /* Filesystem object */
            uint sector		/* Sector number to make appearance in the fs.win[] */
        ) {
            var res = FileResult.Ok;


            if (sector != fs.winsect) {   /* Window offset changed? */

                res = this.SyncWindow(ref fs);      /* Write-back changes */

                if (res == FileResult.Ok) {
                    /* Fill sector window with new data */
                    if (this.diskIO.DiskRead(fs.pdrv, ref fs.win, sector, 1) != DiskResult.Ok) {
                        sector = 0xFFFFFFFF;    /* Invalidate window if read data is not valid */
                        res = FileResult.DiskError;
                    }
                    fs.winsect = sector;
                }
            }
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Synchronize filesystem and data on the storage                        */
        /*-----------------------------------------------------------------------*/

        uint SS(FatFS fs) => FF_MAX_SS;

        FileResult SyncFileSystem( /* Returns FR_OK or FR_DISK_ERR */
            ref FatFS fs       /* Filesystem object */
        ) {
            FileResult res;
            var dummy = new byte[1];

            res = this.SyncWindow(ref fs);
            if (res == FileResult.Ok) {
                if (fs.fs_type == FS_FAT32 && fs.fsi_flag == 1) {   /* FAT32: Update FSInfo sector if needed */
                    /* Create FSInfo structure */
                    SetMemory(ref fs.win, 0, this.SS(fs));
                    StoreWord(ref fs.win, BS_55AA, 0xAA55);
                    StoreDword(ref fs.win, FSI_LeadSig, 0x41615252);
                    StoreDword(ref fs.win, FSI_StrucSig, 0x61417272);
                    StoreDword(ref fs.win, FSI_Free_Count, fs.free_clst);
                    StoreDword(ref fs.win, FSI_Nxt_Free, fs.last_clst);
                    /* Write it into the FSInfo sector */
                    fs.winsect = fs.volbase + 1;
                    this.diskIO.DiskWrite(fs.pdrv, fs.win, fs.winsect, 1);
                    fs.fsi_flag = 0;
                }
                /* Make sure that no pending write process in the lower layer */
                if (this.diskIO.DiskIOControl(fs.pdrv, DiskControl.ControlSync, ref dummy) != DiskResult.Ok) res = FileResult.DiskError;
            }

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Get physical sector number from cluster number                        */
        /*-----------------------------------------------------------------------*/

        static uint ClusterToSector( /* !=0:Sector number, 0:Failed (invalid cluster#) */
            FatFS fs,      /* Filesystem object */
            uint cluster      /* Cluster# to be converted */
        ) {
            cluster -= 2;      /* Cluster number is origin from 2 */
            if (cluster >= fs.n_fatent - 2) return 0;     /* Is it invalid cluster number? */
            return fs.database + fs.csize * cluster;     /* Start sector number of the cluster */
        }

        /*-----------------------------------------------------------------------*/
        /* FAT access - Read value of a FAT entry                                */
        /*-----------------------------------------------------------------------*/

        uint GetFat(       /* 0xFFFFFFFF:Disk error, 1:Internal error, 2..0x7FFFFFFF:Cluster status */
            ref FileObjectIdentifier obj,   /* Corresponding object */
            uint cluster      /* Cluster number to get the value */
        ) {
            uint wc, bc;
            uint val;
            var fs = obj.fs;


            if (cluster < 2 || cluster >= fs.n_fatent) {   /* Check if in valid range */
                val = 1;    /* Internal error */

            }
            else {
                val = 0xFFFFFFFF;   /* Default value falls on disk error */

                switch (fs.fs_type) {
                    case FS_FAT12:
                        bc = cluster; bc += bc / 2;
                        if (this.MoveWindow(ref fs, fs.fatbase + (bc / this.SS(fs))) != FileResult.Ok) break;
                        wc = fs.win[bc++ % this.SS(fs)];        /* Get 1st byte of the entry */
                        if (this.MoveWindow(ref fs, fs.fatbase + (bc / this.SS(fs))) != FileResult.Ok) break;
                        wc |= (uint)fs.win[bc % this.SS(fs)] << 8;    /* Merge 2nd byte of the entry */
                        val = ((cluster & 1) > 1) ? (wc >> 4) : (wc & 0xFFF);    /* Adjust bit position */
                        break;

                    case FS_FAT16:
                        if (this.MoveWindow(ref fs, fs.fatbase + (cluster / (this.SS(fs) / 2))) != FileResult.Ok) break;
                        val = LoadWord(fs.win, cluster * 2 % this.SS(fs));     /* Simple WORD array */
                        break;

                    case FS_FAT32:
                        if (this.MoveWindow(ref fs, fs.fatbase + (cluster / (this.SS(fs) / 4))) != FileResult.Ok) break;
                        val = LoadDword(fs.win, cluster * 4 % this.SS(fs)) & 0x0FFFFFFF;   /* Simple DWORD array but mask out upper 4 bits */
                        break;
                    default:
                        val = 1;    /* Internal error */
                        break;
                }
            }

            return val;
        }

        /*-----------------------------------------------------------------------*/
        /* FAT access - Change value of a FAT entry                              */
        /*-----------------------------------------------------------------------*/

        FileResult PutFat( /* FR_OK(0):succeeded, !=0:error */
            ref FatFS fs,      /* Corresponding filesystem object */
            uint cluster,     /* FAT index number (cluster number) to be changed */
            uint newValue       /* New value to be set to the entry */
        ) {
            uint bc;
            var p = new byte[1];
            var res = FileResult.InternalError;


            if (cluster >= 2 && cluster < fs.n_fatent) {   /* Check if in valid range */
                switch (fs.fs_type) {
                    case FS_FAT12:
                        bc = (uint)cluster; bc += bc / 2;  /* bc: byte offset of the entry */
                        res = this.MoveWindow(ref fs, fs.fatbase + (bc / this.SS(fs)));
                        if (res != FileResult.Ok) break;
                        p[0] = fs.win[bc++ % this.SS(fs)];
                        p[0] = (byte)(((cluster & 1) > 1) ? ((p[0] & 0x0F) | ((byte)newValue << 4)) : (byte)newValue);     /* Put 1st byte */
                        fs.wflag = 1;
                        res = this.MoveWindow(ref fs, fs.fatbase + (bc / this.SS(fs)));
                        if (res != FileResult.Ok) break;
                        p[0] = fs.win[bc % this.SS(fs)];
                        p[0] = (byte)(((cluster & 1) > 1) ? (byte)(newValue >> 4) : ((p[0] & 0xF0) | ((byte)(newValue >> 8) & 0x0F))); /* Put 2nd byte */
                        fs.wflag = 1;
                        break;

                    case FS_FAT16:
                        res = this.MoveWindow(ref fs, fs.fatbase + (cluster / (this.SS(fs) / 2)));
                        if (res != FileResult.Ok) break;
                        StoreWord(ref fs.win, cluster * 2 % this.SS(fs), (uint)newValue);    /* Simple WORD array */
                        fs.wflag = 1;
                        break;

                    case FS_FAT32:
                        res = this.MoveWindow(ref fs, fs.fatbase + (cluster / (this.SS(fs) / 4)));
                        if (res != FileResult.Ok) break;
                        if (fs.fs_type != FS_EXFAT) {
                            newValue = (newValue & 0x0FFFFFFF) | (LoadDword(fs.win, cluster * 4 % this.SS(fs)) & 0xF0000000);
                        }
                        StoreDword(ref fs.win, cluster * 4 % this.SS(fs), newValue);
                        fs.wflag = 1;
                        break;
                }
            }
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* FAT handling - Remove a cluster chain                                 */
        /*-----------------------------------------------------------------------*/

        FileResult RemoveChain(    /* FR_OK(0):succeeded, !=0:error */
            ref FileObjectIdentifier obj,       /* Corresponding object */
            uint clst,         /* Cluster to remove a chain from */
            uint pclst         /* Previous cluster of clst (0:entire chain) */
        ) {
            var res = FileResult.Ok;
            uint nxt;
            var fs = obj.fs;

            if (clst < 2 || clst >= fs.n_fatent) return FileResult.InternalError;    /* Check if in valid range */

            /* Mark the previous cluster 'EOC' on the FAT if it exists */
            if (pclst != 0 && (fs.fs_type != FS_EXFAT || obj.stat != 2)) {
                res = this.PutFat(ref fs, pclst, 0xFFFFFFFF);
                if (res != FileResult.Ok) return res;
            }

            /* Remove the chain */
            do {
                nxt = this.GetFat(ref obj, clst);           /* Get cluster status */
                if (nxt == 0) break;                /* Empty cluster? */
                if (nxt == 1) return FileResult.InternalError;    /* Internal error? */
                if (nxt == 0xFFFFFFFF) return FileResult.DiskError;  /* Disk error? */
                if (fs.fs_type != FS_EXFAT) {
                    res = this.PutFat(ref fs, clst, 0);     /* Mark the cluster 'free' on the FAT */
                    if (res != FileResult.Ok) return res;
                }
                if (fs.free_clst < fs.n_fatent - 2) {   /* Update FSINFO */
                    fs.free_clst++;
                    fs.fsi_flag |= 1;
                }
                clst = nxt;                 /* Next cluster */
            } while (clst < fs.n_fatent);  /* Repeat while not the last link */
            return FileResult.Ok;
        }

        /*-----------------------------------------------------------------------*/
        /* FAT handling - Stretch a chain or Create a new chain                  */
        /*-----------------------------------------------------------------------*/

        uint CreateChain(  /* 0:No free cluster, 1:Internal error, 0xFFFFFFFF:Disk error, >=2:New cluster# */
            ref FileObjectIdentifier obj,       /* Corresponding object */
            uint cluster          /* Cluster# to stretch, 0:Create a new chain */
        ) {
            uint cs, ncl, scl;
            FileResult res;
            var fs = obj.fs;


            if (cluster == 0) {   /* Create a new chain */
                scl = fs.last_clst;                /* Suggested cluster to start to find */
                if (scl == 0 || scl >= fs.n_fatent) scl = 1;
            }
            else {
                /* Stretch a chain */
                cs = this.GetFat(ref obj, cluster);            /* Check the cluster status */
                if (cs < 2) return 1;               /* Test for insanity */
                if (cs == 0xFFFFFFFF) return cs;    /* Test for disk error */
                if (cs < fs.n_fatent) return cs;   /* It is already followed by next cluster */
                scl = cluster;                         /* Cluster to start to find */
            }
            if (fs.free_clst == 0) return 0;       /* No free cluster */
            {   /* On the FAT/FAT32 volume */
                ncl = 0;
                if (scl == cluster) {                       /* Stretching an existing chain? */
                    ncl = scl + 1;                      /* Test if next cluster is free */
                    if (ncl >= fs.n_fatent) ncl = 2;
                    cs = this.GetFat(ref obj, ncl);             /* Get next cluster status */
                    if (cs == 1 || cs == 0xFFFFFFFF) return cs; /* Test for error */
                    if (cs != 0) {                       /* Not free? */
                        cs = fs.last_clst;             /* Start at suggested cluster if it is valid */
                        if (cs >= 2 && cs < fs.n_fatent) scl = cs;
                        ncl = 0;
                    }
                }
                if (ncl == 0) {   /* The new cluster cannot be contiguous and find another fragment */
                    ncl = scl;  /* Start cluster */
                    for (; ; )
                    {
                        ncl++;                          /* Next cluster */
                        if (ncl >= fs.n_fatent) {
                            /* Check wrap-around */
                            ncl = 2;
                            if (ncl > scl) return 0;    /* No free cluster found? */
                        }
                        cs = this.GetFat(ref obj, ncl);         /* Get the cluster status */
                        if (cs == 0) break;             /* Found a free cluster? */
                        if (cs == 1 || cs == 0xFFFFFFFF) return cs; /* Test for error */
                        if (ncl == scl) return 0;       /* No free cluster found? */
                    }
                }
                res = this.PutFat(ref fs, ncl, 0xFFFFFFFF);     /* Mark the new cluster 'EOC' */
                if (res == FileResult.Ok && cluster != 0) {
                    res = this.PutFat(ref fs, cluster, ncl);       /* Link it from the previous one if needed */
                }
            }

            if (res == FileResult.Ok) {
                /* Update FSINFO if function succeeded. */
                fs.last_clst = ncl;
                if (fs.free_clst <= fs.n_fatent - 2) fs.free_clst--;
                fs.fsi_flag |= 1;
            }
            else {
                ncl = (res == FileResult.DiskError) ? 0xFFFFFFFF : 1;    /* Failed. Generate error status */
            }

            return ncl;     /* Return new cluster number or error status */
        }

        /*-----------------------------------------------------------------------*/
        /* Directory handling - Fill a cluster with zeros                        */
        /*-----------------------------------------------------------------------*/

        FileResult ClearDirectory(   /* Returns FR_OK or FR_DISK_ERR */
            ref FatFS fs,      /* Filesystem object */
            uint cluster      /* Directory table to clear */
        ) {
            uint sect;
            uint n, szb;
            byte[] ibuf;


            if (this.SyncWindow(ref fs) != FileResult.Ok) return FileResult.DiskError;   /* Flush disk access window */
            sect = ClusterToSector(fs, cluster);     /* Top of the cluster */
            fs.winsect = sect;             /* Set window to top of the cluster */
            SetMemory(ref fs.win, 0, this.SS(fs));    /* Clear window buffer */
            {
                ibuf = fs.win; szb = 1;    /* Use window buffer (many single-sector writes may take a time) */
                for (n = 0; n < fs.csize && this.diskIO.DiskWrite(fs.pdrv, ibuf, sect + n, szb) == DiskResult.Ok; n += szb) ;   /* Fill the cluster with 0 */
            }
            return (n == fs.csize) ? FileResult.Ok : FileResult.DiskError;
        }

        /*-----------------------------------------------------------------------*/
        /* Directory handling - Set directory index                              */
        /*-----------------------------------------------------------------------*/

        FileResult SetDirectoryIndex( /* FR_OK(0):succeeded, !=0:error */
            ref DirectoryObject dp,        /* Pointer to directory object */
            uint ofs       /* Offset of directory table */
        ) {
            uint csz, clst;
            var fs = dp.obj.fs;


            if (ofs >= MAX_DIR || ofs % SZDIRE > 0) {   /* Check range of offset and alignment */
                return FileResult.InternalError;
            }
            dp.dptr = ofs;             /* Set current offset */
            clst = dp.obj.sclust;      /* Table start cluster (0:root) */
            if (clst == 0 && fs.fs_type >= FS_FAT32) {   /* Replace cluster# 0 with root cluster# */
                clst = fs.dirbase;
            }

            if (clst == 0) {   /* Static table (root-directory on the FAT volume) */
                if (ofs / SZDIRE >= fs.n_rootdir) return FileResult.InternalError;   /* Is index out of range? */
                dp.sect = fs.dirbase;
            }
            else {
                /* Dynamic table (sub-directory or root-directory on the FAT32/exFAT volume) */
                csz = (uint)fs.csize * this.SS(fs);    /* Bytes per cluster */
                while (ofs >= csz) {
                    /* Follow cluster chain */
                    clst = this.GetFat(ref dp.obj, clst);             /* Get next cluster */
                    if (clst == 0xFFFFFFFF) return FileResult.DiskError; /* Disk error */
                    if (clst < 2 || clst >= fs.n_fatent) return FileResult.InternalError;    /* Reached to end of table or internal error */
                    ofs -= csz;
                }
                dp.sect = ClusterToSector(fs, clst);
            }
            dp.clust = clst;                   /* Current cluster# */
            if (dp.sect == 0) return FileResult.InternalError;
            dp.sect += ofs / this.SS(fs);           /* Sector# of the directory entry */
            dp.dirAsFsWinOffset = ofs % this.SS(fs); // New: fs.win offset to the entry /* Pointer to the entry in the win[] */

            return FileResult.Ok;
        }


        /*-----------------------------------------------------------------------*/
        /* Directory handling - Move directory table index next                  */
        /*-----------------------------------------------------------------------*/

        FileResult NextDirectory(    /* FR_OK(0):succeeded, FR_NO_FILE:End of table, FR_DENIED:Could not stretch */
            ref DirectoryObject dp,                /* Pointer to the directory object */
            int stretch             /* 0: Do not stretch table, 1: Stretch table if needed */
        ) {
            uint ofs, clst;
            var fs = dp.obj.fs;


            ofs = dp.dptr + SZDIRE;    /* Next entry */
            if (dp.sect == 0 || ofs >= MAX_DIR) return FileResult.FileNotExist;    /* Report EOT when offset has reached max value */

            if (ofs % this.SS(fs) == 0) {   /* Sector changed? */
                dp.sect++;             /* Next sector */

                if (dp.clust == 0) {   /* Static table */
                    if (ofs / SZDIRE >= fs.n_rootdir) {   /* Report EOT if it reached end of static table */
                        dp.sect = 0; return FileResult.FileNotExist;
                    }
                }
                else {                   /* Dynamic table */
                    if ((ofs / this.SS(fs) & (fs.csize - 1)) == 0) {   /* Cluster changed? */
                        clst = this.GetFat(ref dp.obj, dp.clust);        /* Get next cluster */
                        if (clst <= 1) return FileResult.InternalError;           /* Internal error */
                        if (clst == 0xFFFFFFFF) return FileResult.DiskError; /* Disk error */
                        if (clst >= fs.n_fatent) {
                            /* It reached end of dynamic table */
                            if (stretch == 0) {
                                /* If no stretch, report EOT */
                                dp.sect = 0; return FileResult.FileNotExist;
                            }
                            clst = this.CreateChain(ref dp.obj, dp.clust);   /* Allocate a cluster */
                            if (clst == 0) return FileResult.AccessDenied;            /* No free cluster */
                            if (clst == 1) return FileResult.InternalError;           /* Internal error */
                            if (clst == 0xFFFFFFFF) return FileResult.DiskError; /* Disk error */
                            if (this.ClearDirectory(ref fs, clst) != FileResult.Ok) return FileResult.DiskError;   /* Clean up the stretched table */
                        }
                        dp.clust = clst;       /* Initialize data for new cluster */
                        dp.sect = ClusterToSector(fs, clst);
                    }
                }
            }
            dp.dptr = ofs;                     /* Current entry */
            dp.dirAsFsWinOffset = ofs % this.SS(fs);   // New: Offset from fs.win to entry /* Pointer to the entry in the win[] */

            return FileResult.Ok;
        }

        /*-----------------------------------------------------------------------*/
        /* Directory handling - Reserve a block of directory entries             */
        /*-----------------------------------------------------------------------*/

        FileResult AllocateDirectoryBlock(   /* FR_OK(0):succeeded, !=0:error */
            ref DirectoryObject dp,                /* Pointer to the directory object */
            uint nent               /* Number of contiguous entries to allocate */
        ) {
            FileResult res;
            uint n;
            var fs = dp.obj.fs;


            res = this.SetDirectoryIndex(ref dp, 0);
            if (res == FileResult.Ok) {
                n = 0;
                do {
                    res = this.MoveWindow(ref fs, dp.sect);
                    if (res != FileResult.Ok) break;

                    if (fs.win[dp.dirAsFsWinOffset + DIR_Name] == DDEM || fs.win[dp.dirAsFsWinOffset + DIR_Name] == 0)    // HB: Check if this works
                    {
                        if (++n == nent) break; /* A block of contiguous free entries is found */
                    }
                    else {
                        n = 0;                  /* Not a blank entry. Restart to search */
                    }
                    res = this.NextDirectory(ref dp, 1);
                } while (res == FileResult.Ok); /* Next entry with table stretch enabled */
            }

            if (res == FileResult.FileNotExist) res = FileResult.AccessDenied; /* No directory entry to allocate */
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* FAT: Directory handling - Load/Store start cluster number             */
        /*-----------------------------------------------------------------------*/

        uint LoadCluster(   /* Returns the top cluster value of the SFN entry */
            FatFS fs,			/* Pointer to the fs object */
            byte[] dir		    /* Pointer to the key entry */
        ) {

            uint cl;

            cl = LoadWord(dir, DIR_FstClusLO);
            if (fs.fs_type == FS_FAT32) {
                cl |= (uint)LoadWord(dir, DIR_FstClusHI) << 16;
            }

            return cl;
        }

        uint LoadCluster(   /* Returns the top cluster value of the SFN entry */
            FatFS fs,           /* Pointer to the fs object */
            byte[] buff,          /* Pointer to the key entry */
            uint buffOffset      /* Offset into buff where to set cluster value */
        ) {

            uint cl;

            cl = LoadWord(buff, buffOffset + DIR_FstClusLO);
            if (fs.fs_type == FS_FAT32) {
                cl |= (uint)LoadWord(buff, buffOffset + DIR_FstClusHI) << 16;
            }

            return cl;
        }



        void StoreCluster(
            ref FatFS fs,  /* Pointer to the fs object */
            uint winDirOffset,  /* Pointer to the key entry */
            uint cl	/* Value to be set */
        ) {
            StoreWord(ref fs.win, winDirOffset + DIR_FstClusLO, (uint)cl);
            if (fs.fs_type == FS_FAT32) {
                StoreWord(ref fs.win, winDirOffset + DIR_FstClusHI, (uint)(cl >> 16));
            }
        }



        /*-----------------------------------------------------------------------*/
        /* Directory handling - Find an object in the directory                  */
        /*-----------------------------------------------------------------------*/

        FileResult FindObjectInDirectory(    /* FR_OK(0):succeeded, !=0:error */
            ref DirectoryObject dp                 /* Pointer to the directory object with the file name */
        ) {
            FileResult res;
            var fs = dp.obj.fs;
            byte c;

            res = this.SetDirectoryIndex(ref dp, 0);           /* Rewind directory object */
            if (res != FileResult.Ok) return res;

            /* On the FAT/FAT32 volume */
            do {
                res = this.MoveWindow(ref fs, dp.sect);
                if (res != FileResult.Ok) break;
                c = fs.win[dp.dirAsFsWinOffset + DIR_Name]; // HB: Test this
                if (c == 0) { res = FileResult.FileNotExist; break; }    /* Reached to end of table */


                dp.obj.attr = (byte)(fs.win[dp.dirAsFsWinOffset + DIR_Attr] & AM_MASK); // HB: Test this
                var fsFilename = new byte[11];
                Array.Copy(fs.win, (int)dp.dirAsFsWinOffset, fsFilename, 0, 11);
                if (((fs.win[dp.dirAsFsWinOffset + DIR_Attr] & AM_VOL) == 0) && CompareMemory(fsFilename, dp.fn, 11) == 0) break;  /* Is it a valid entry? */

                res = this.NextDirectory(ref dp, 0);  /* Next entry */
            } while (res == FileResult.Ok);

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Read an object from the directory                                     */
        /*-----------------------------------------------------------------------*/

        public FileResult ReadFileInDirectory(ref DirectoryObject dp) => this.Dir_read(ref dp, 0);
        public FileResult ReadVolumeLabel(ref DirectoryObject dp) => this.Dir_read(ref dp, 1);

        FileResult Dir_read(
            ref DirectoryObject dp,         /* Pointer to the directory object */
            int vol             /* Filtered by 0:file/directory or 1:volume label */
        ) {
            var res = FileResult.FileNotExist;
            var fs = dp.obj.fs;
            byte a, c;

            while (dp.sect > 0) {
                res = this.MoveWindow(ref fs, dp.sect);
                if (res != FileResult.Ok) break;
                c = fs.win[dp.dirAsFsWinOffset + DIR_Name];  /* Test for the entry type */
                if (c == 0) {
                    res = FileResult.FileNotExist; break; /* Reached to end of the directory */
                }


                dp.obj.attr = a = (byte)(fs.win[dp.dirAsFsWinOffset + DIR_Attr] & AM_MASK); /* Get attribute */

                if (c != DDEM && c != '.' && a != AM_LFN && (((a & ~AM_ARC) == AM_VOL) ? 1 : 0) == vol)   // HB : Changed
                {   /* Is it a valid entry? */
                    break;
                }


                res = this.NextDirectory(ref dp, 0);      /* Next entry */
                if (res != FileResult.Ok) break;
            }

            if (res != FileResult.Ok) dp.sect = 0;     /* Terminate the read operation on error or EOT */
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Register an object to the directory                                   */
        /*-----------------------------------------------------------------------*/

        FileResult RegisterDirectoryObject(    /* FR_OK:succeeded, FR_DENIED:no free entry or too many SFN collision, FR_DISK_ERR:disk error */
            ref DirectoryObject dp                     /* Target directory with object name to be created */
        ) {
            FileResult res;
            var fs = dp.obj.fs;

            res = this.AllocateDirectoryBlock(ref dp, 1);     /* Allocate an entry for SFN */

            /* Set SFN entry */
            if (res == FileResult.Ok) {
                res = this.MoveWindow(ref fs, dp.sect);
                if (res == FileResult.Ok) {
                    SetMemory(ref fs.win, (int)dp.dirAsFsWinOffset, 0, SZDIRE);    /* Clean the entry */
                    CopyMemory(ref fs.win, (int)(dp.dirAsFsWinOffset + DIR_Name), dp.fn, 11);    /* Put SFN */

                    fs.wflag = 1;
                }
            }
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Remove an object from the directory                                   */
        /*-----------------------------------------------------------------------*/

        FileResult RemoveFromDirectory(  /* FR_OK:Succeeded, FR_DISK_ERR:A disk error */
            ref DirectoryObject dp                 /* Directory object pointing the entry to be removed */
        ) {
            FileResult res;
            var fs = dp.obj.fs;

            res = this.MoveWindow(ref fs, dp.sect);
            if (res == FileResult.Ok) {
                fs.win[dp.dirAsFsWinOffset + DIR_Name] = DDEM;   /* Mark the entry 'deleted'.*/
                fs.wflag = 1;
            }
            return res;
        }


        /*-----------------------------------------------------------------------*/
        /* Get file information from directory entry                             */
        /*-----------------------------------------------------------------------*/

        void GetFileInfo(
            DirectoryObject dp,            /* Pointer to the directory object */
            ref FileInfo fno        /* Pointer to the file information to be filled */
        ) {
            uint si, di;
            byte c;
            var fs = dp.obj.fs;

            fno.fileName[0] = 0;          /* Invalidate file info */
            if (dp.sect == 0) return;  /* Exit if read pointer has reached end of directory */


            si = di = 0;
            while (si < 11) {
                /* Copy name body and extension */
                c = (byte)fs.win[dp.dirAsFsWinOffset + si++];
                if (c == ' ') continue;     /* Skip padding spaces */
                if (c == RDDEM) c = DDEM;   /* Restore replaced DDEM character */
                if (si == 9) fno.fileName[di++] = (byte)'.';  /* Insert a . if extension is exist */
                fno.fileName[di++] = c;
            }
            fno.fileName[di] = 0;


            fno.fileAttribute = fs.win[dp.dirAsFsWinOffset + DIR_Attr];                   /* Attribute */
            fno.fileSize = LoadDword(fs.win, dp.dirAsFsWinOffset + DIR_FileSize);      /* Size */
            fno.fileTime = LoadWord(fs.win, dp.dirAsFsWinOffset + DIR_ModTime + 0);    /* Time */
            fno.fileDate = LoadWord(fs.win, dp.dirAsFsWinOffset + DIR_ModTime + 2);    /* Date */
        }

        /*-----------------------------------------------------------------------*/
        /* Pick a top segment and create the object name in directory form       */
        /*-----------------------------------------------------------------------*/

        FileResult CreateObjectName( /* FR_OK: successful, FR_INVALID_NAME: could not create */
            ref DirectoryObject dp,				/* Pointer to the directory object */
            byte[] path,			/* Pointer to start of the path string */
            ref uint pathIndex      // Current offset in path (all before offset has been evaluated already)
        ) {

            byte c, d;
            byte[] sfn;
            uint ni, si, i;

            /* Create file name in directory form */
            sfn = dp.fn;
            SetMemory(ref sfn, ' ', 11);
            si = i = 0; ni = 8;
            for (; ; )
            {
                c = (byte)path[pathIndex + si++];             /* Get a byte */
                if (c <= ' ') break;            /* Break if end of the path name */
                if (c == '/' || c == '\\') {   /* Break if a separator is found */
                    while (path[pathIndex + si] == '/' || path[pathIndex + si] == '\\') si++;   /* Skip duplicated separator if exist */
                    break;
                }
                if (c == '.' || i >= ni) {       /* End of body or field overflow? */
                    if (ni == 11 || c != '.') return FileResult.InvalidPathName;   /* Field overflow or invalid dot? */
                    i = 8; ni = 11;             /* Enter file extension field */
                    continue;
                }
                if (CheckDBCFirstByte(c) > 0) {               /* Check if it is a DBC 1st byte */
                    d = (byte)path[pathIndex + si++];         /* Get 2nd byte */
                    if ((CheckDBCSecondByte(d) == 0) || i >= ni - 1) return FileResult.InvalidPathName;   /* Reject invalid DBC */
                    sfn[i++] = c;
                    sfn[i++] = d;
                }
                else {
                    /* SBC */
                    if (ContainsChar(@"\ * +,:;<=>\?[]|", c) > 0) return FileResult.InvalidPathName;    /* Reject illegal chrs for SFN */
                    if (IsLower((char)c)) c -= 0x20;    /* To upper */
                    sfn[i++] = c;
                }
            }

            pathIndex = pathIndex + si;                     /* Return pointer to the next segment */
            if (i == 0) return FileResult.InvalidPathName;     /* Reject nul string */

            if (sfn[0] == DDEM) sfn[0] = RDDEM; /* If the first character collides with DDEM, replace it with RDDEM */
            sfn[NSFLAG] = (c <= (byte)' ') ? NS_LAST : (byte)0;     /* Set last segment flag if end of the path */

            return FileResult.Ok;

        }

        /*-----------------------------------------------------------------------*/
        /* Follow a file path                                                    */
        /*-----------------------------------------------------------------------*/

        FileResult FollowFilePath( /* FR_OK(0): successful, !=0: error code */
            ref DirectoryObject dp,					/* Directory object to return last directory and found object */
            byte[] path,			/* Full-path string to find a file or directory */
            ref uint pathIndex      // Current offset in path (all before offset has been evaluated already)
        ) {

            FileResult res;
            byte ns;
            var fs = dp.obj.fs;


            while (path[pathIndex] == '/' || path[pathIndex] == '\\') pathIndex++;  /* Strip heading separator */
            dp.obj.sclust = 0;                  /* Start from root directory */

            if (path[pathIndex] < ' ') {
                /* Null path name is the origin directory itself */
                dp.fn[NSFLAG] = NS_NONAME;
                res = this.SetDirectoryIndex(ref dp, 0);
            }
            else {
                /* Follow path */
                for (; ; )
                {
                    res = this.CreateObjectName(ref dp, path, ref pathIndex); /* Get a segment name of the path */
                    if (res != FileResult.Ok) break;
                    res = this.FindObjectInDirectory(ref dp);             /* Find an object with the segment name */
                    ns = dp.fn[NSFLAG];
                    if (res != FileResult.Ok) {               /* Failed to find the object */
                        if (res == FileResult.FileNotExist) {    /* Object is not found */
                            if ((ns & NS_LAST) == 0) res = FileResult.PathNotFound;  /* Adjust error code if not last segment */
                        }
                        break;
                    }
                    if ((ns & NS_LAST) > 0) break;          /* Last segment matched. Function completed. */
                    /* Get into the sub-directory */
                    if ((dp.obj.attr & AM_DIR) == 0) {       /* It is not a sub-directory and cannot follow */
                        res = FileResult.PathNotFound; break;
                    }


                    dp.obj.sclust = this.LoadCluster(fs, fs.win.SubArray(dp.dptr % this.SS(fs)));	/* Open next directory */ // HB: Check
                }
            }
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Get logical drive number from path name                               */
        /*-----------------------------------------------------------------------*/

        int GetLogicalDriveNumber(	/* Returns logical drive number (-1:invalid drive number or null pointer) */
            byte[] path,		/* Pointer to pointer to the path name */
            ref uint pathIndex
        ) {

            uint tp, tt;
            char tc, c;
            int i, vol = -1;
            char[] sp;
            uint spIndex = 0;

            tt = tp = pathIndex;
            if (path.Length == 0) return vol; /* Invalid path name? */
            do {
                tc = (char)path[tt++];
            }
            while (tc >= '!' && tc != ':'); /* Find a colon in the path */

            if (tc == ':') {
                /* DOS/Windows style volume ID? */
                i = FF_VOLUMES;
                if (IsDigit((char)path[tp]) && tp + 2 == tt) {   /* Is there a numeric volume ID + colon? */
                    i = (int)path[tp] - '0';   /* Get the LD number */
                }
                else {
                    i = 0;
                    do {
                        sp = volumeStr[i].ToCharArray(); tp = pathIndex;    /* This string volume ID and path name */
                        do {
                            /* Compare the volume ID with path name */
                            c = sp[spIndex++]; tc = (char)path[tp++];
                            if (IsLower(c)) c -= (char)0x20;
                            if (IsLower(tc)) tc -= (char)0x20;
                        } while (c > 0 && c == tc);
                    } while ((c > 0 || tp != tt) && ++i < FF_VOLUMES);  /* Repeat for each id until pattern match */
                }

                if (i < FF_VOLUMES) {
                    /* If a volume ID is found, get the drive number and strip it */
                    vol = i;		/* Drive number */
                    pathIndex = tt;     /* Snip the drive prefix off */
                }
                return vol;
            }
            /* No drive prefix is found */
            vol = 0;        /* Default drive is 0 */
            return vol;		/* Return the default drive */
        }

        /*-----------------------------------------------------------------------*/
        /* Load a sector and check if it is an FAT VBR                           */
        /*-----------------------------------------------------------------------*/

        byte CheckFileSystem(   /* 0:FAT, 1:exFAT, 2:Valid BS but not FAT, 3:Not a BS, 4:Disk error */
            ref FatFS fs,          /* Filesystem object */
            uint sector          /* Sector# (lba) to load and check if it is an FAT-VBR or not */
        ) {
            fs.wflag = 0; fs.winsect = 0xFFFFFFFF;        /* Invalidate window */
            if (this.MoveWindow(ref fs, sector) != FileResult.Ok) return 4;   /* Load boot record */

            if (LoadWord(fs.win, BS_55AA) != 0xAA55) return 3; /* Check boot record signature (always here regardless of the sector size) */

            if (fs.win[BS_JmpBoot] == 0xE9 || fs.win[BS_JmpBoot] == 0xEB || fs.win[BS_JmpBoot] == 0xE8) {   /* Valid JumpBoot code? */
                if (CompareMemory(fs.win.Slice(BS_FilSysType, 3), Encoding.UTF8.GetBytes("FAT"), 3) == 0) return 0;      /* Is it an FAT VBR? */
                if (CompareMemory(fs.win.Slice(BS_FilSysType32, 5), Encoding.UTF8.GetBytes("FAT32"), 5) == 0) return 0;  /* Is it an FAT32 VBR? */
            }
            return 2;   /* Valid BS but not FAT */
        }

        /*-----------------------------------------------------------------------*/
        /* Determine logical drive number and mount the volume if needed         */
        /*-----------------------------------------------------------------------*/

        FileResult FindVolume(	/* FR_OK(0): successful, !=0: an error occurred */

            ref byte[] path,            /* Pointer to pointer to the path name (drive number) */
            ref FatFS rfs,              /* Pointer to pointer to the found filesystem object */
            byte mode					/* !=0: Check write protection for write access */
        ) {

            byte fmt;
            byte[] pt;
            int vol;
            byte stat;
            uint bsect, fasize, tsect, sysect, nclst, szbfat;
            var br = new uint[4];
            uint nrsv;
            FatFS fs;
            uint i;
            uint pathIndex = 0;


            /* Get logical drive number */
            rfs = null;
            vol = this.GetLogicalDriveNumber(path, ref pathIndex);
            if (vol < 0) return FileResult.InvalidDrive;

            /* Check if the filesystem object is valid or not */
            fs = fatFs[vol];                    /* Get pointer to the filesystem object */
            if (fs == null) return FileResult.NotEnabled;      /* Is the filesystem object available? */

            rfs = fs;							/* Return pointer to the filesystem object */

            mode &= (byte)(~FA_READ & 0xff);                /* Desired access mode, write access or not */
            if (fs.fs_type != 0) {               /* If the volume has been mounted */
                stat = this.diskIO.DiskStatus(fs.pdrv);
                if ((stat & STA_NOINIT) == 0) {
                    /* and the physical drive is kept initialized */
                    if (mode > 0 && (stat & STA_PROTECT) > 0) {   /* Check write protection if needed */
                        return FileResult.WriteProtected;
                    }
                    return FileResult.Ok;               /* The filesystem object is valid */
                }
            }

            /* The filesystem object is not valid. */
            /* Following code attempts to mount the volume. (analyze BPB and initialize the filesystem object) */

            fs.fs_type = 0;                 /* Clear the filesystem object */
            fs.pdrv = (byte)vol;              /* Bind the logical drive and a physical drive */
            stat = this.diskIO.DiskInit(fs.pdrv); /* Initialize the physical drive */
            if ((stat & STA_NOINIT) > 0) {           /* Check if the initialization succeeded */
                return FileResult.NotReady;            /* Failed to initialize due to no medium or hard error */
            }
            if (mode > 0 && (stat & STA_PROTECT) > 0) { /* Check disk write protection if needed */
                return FileResult.WriteProtected;
            }


            /* Find an FAT partition on the drive. Supports only generic partitioning rules, FDISK and SFD. */
            bsect = 0;
            fmt = this.CheckFileSystem(ref fs, bsect);          /* Load sector 0 and check if it is an FAT-VBR as SFD */
            if (fmt == 2 || (fmt < 2 && (byte)vol != 0)) {   /* Not an FAT-VBR or forced partition number */
                for (i = 0; i < 4; i++) {       /* Get partition offset */
                    pt = fs.win.SubArray(MBR_Table + i * SZ_PTE);
                    br[i] = (pt[PTE_System] > 0) ? LoadDword(pt, PTE_StLba) : 0;
                }
                i = (byte)vol;                    /* Partition number: 0:auto, 1-4:forced */
                if (i != 0) i--;
                do {                           /* Find an FAT volume */
                    bsect = br[i];
                    fmt = (bsect > 0) ? this.CheckFileSystem(ref fs, bsect) : (byte)3;  /* Check the partition */
                } while ((byte)vol == 0 && fmt >= 2 && ++i < 4);
            }
            if (fmt == 4) return FileResult.DiskError;       /* An error occured in the disk I/O layer */
            if (fmt >= 2) return FileResult.NoFileSystem;  /* No FAT volume is found */

            /* An FAT volume is found (bsect). Following code initializes the filesystem object */

            if (LoadWord(fs.win, BPB_BytsPerSec) != this.SS(fs)) return FileResult.NoFileSystem; /* (BPB_BytsPerSec must be equal to the physical sector size) */

            fasize = LoadWord(fs.win, BPB_FATSz16);      /* Number of sectors per FAT */
            if (fasize == 0) fasize = LoadDword(fs.win, BPB_FATSz32);
            fs.fsize = fasize;

            fs.n_fats = fs.win[BPB_NumFATs];                /* Number of FATs */
            if (fs.n_fats != 1 && fs.n_fats != 2) return FileResult.NoFileSystem;  /* (Must be 1 or 2) */
            fasize *= fs.n_fats;                            /* Number of sectors for FAT area */

            fs.csize = fs.win[BPB_SecPerClus];          /* Cluster size */
            if (fs.csize == 0 || (fs.csize & (fs.csize - 1)) > 0) return FileResult.NoFileSystem;  /* (Must be power of 2) */

            fs.n_rootdir = LoadWord(fs.win, BPB_RootEntCnt); /* Number of root directory entries */
            if (fs.n_rootdir % (this.SS(fs) / SZDIRE) > 0) return FileResult.NoFileSystem;  /* (Must be sector aligned) */

            tsect = LoadWord(fs.win, BPB_TotSec16);      /* Number of sectors on the volume */
            if (tsect == 0) tsect = LoadDword(fs.win, BPB_TotSec32);

            nrsv = LoadWord(fs.win, BPB_RsvdSecCnt);     /* Number of reserved sectors */
            if (nrsv == 0) return FileResult.NoFileSystem;         /* (Must not be 0) */

            /* Determine the FAT sub type */
            sysect = nrsv + fasize + fs.n_rootdir / (this.SS(fs) / SZDIRE);  /* RSV + FAT + DIR */
            if (tsect < sysect) return FileResult.NoFileSystem;    /* (Invalid volume size) */
            nclst = (tsect - sysect) / fs.csize;            /* Number of clusters */
            if (nclst == 0) return FileResult.NoFileSystem;        /* (Invalid volume size) */
            fmt = 0;
            if (nclst <= MAX_FAT32) fmt = FS_FAT32;
            if (nclst <= MAX_FAT16) fmt = FS_FAT16;
            if (nclst <= MAX_FAT12) fmt = FS_FAT12;
            if (fmt == 0) return FileResult.NoFileSystem;

            /* Boundaries and Limits */
            fs.n_fatent = nclst + 2;                        /* Number of FAT entries */
            fs.volbase = bsect;                         /* Volume start sector */
            fs.fatbase = bsect + nrsv;                  /* FAT start sector */
            fs.database = bsect + sysect;                   /* Data start sector */
            if (fmt == FS_FAT32) {
                if (LoadWord(fs.win, BPB_FSVer32) != 0) return FileResult.NoFileSystem; /* (Must be FAT32 revision 0.0) */
                if (fs.n_rootdir != 0) return FileResult.NoFileSystem; /* (BPB_RootEntCnt must be 0) */
                fs.dirbase = LoadDword(fs.win, BPB_RootClus32);   /* Root directory start cluster */
                szbfat = fs.n_fatent * 4;                   /* (Needed FAT size) */
            }
            else {
                if (fs.n_rootdir == 0) return FileResult.NoFileSystem; /* (BPB_RootEntCnt must not be 0) */
                fs.dirbase = fs.fatbase + fasize;           /* Root directory start sector */
                szbfat = (fmt == FS_FAT16) ?				/* (Needed FAT size) */

                    fs.n_fatent * 2 : fs.n_fatent * 3 / 2 + (fs.n_fatent & 1);
            }
            if (fs.fsize < (szbfat + (this.SS(fs) - 1)) / this.SS(fs)) return FileResult.NoFileSystem;   /* (BPB_FATSz must not be less than the size needed) */


            /* Get FSInfo if available */
            fs.last_clst = fs.free_clst = 0xFFFFFFFF;       /* Initialize cluster allocation information */
            fs.fsi_flag = 0x80;

            if (fmt == FS_FAT32             /* Allow to update FSInfo only if BPB_FSInfo32 == 1 */
                && LoadWord(fs.win, BPB_FSInfo32) == 1
                && this.MoveWindow(ref fs, bsect + 1) == FileResult.Ok) {
                fs.fsi_flag = 0;
                if (LoadWord(fs.win, BS_55AA) == 0xAA55  /* Load FSInfo data if available */
                    && LoadDword(fs.win, FSI_LeadSig) == 0x41615252
                    && LoadDword(fs.win, FSI_StrucSig) == 0x61417272) {
                    fs.free_clst = LoadDword(fs.win, FSI_Free_Count);
                    fs.last_clst = LoadDword(fs.win, FSI_Nxt_Free);
                }
            }



            fs.fs_type = fmt;       /* FAT sub-type */
            fs.id = ++fsid;     /* Volume mount ID */


            return FileResult.Ok;
        }


        /*-----------------------------------------------------------------------*/
        /* Check if the file/directory object is valid or not                    */
        /*-----------------------------------------------------------------------*/

        FileResult ValidateObject(    /* Returns FR_OK or FR_INVALID_OBJECT */
            ref FileObjectIdentifier obj,           /* Pointer to the FFOBJID, the 1st member in the FIL/DIR object, to check validity */
            ref FatFS rfs             /* Pointer to pointer to the owner filesystem object to return */
        ) {
            var res = FileResult.InvalidObject;


            if (obj != null && obj.fs != null && obj.fs.fs_type > 0 && obj.id == obj.fs.id) {   /* Test if the object is valid */

                if ((this.diskIO.DiskStatus(obj.fs.pdrv) & STA_NOINIT) == 0) { /* Test if the phsical drive is kept initialized */
                    res = FileResult.Ok;
                }

            }
            rfs = (res == FileResult.Ok) ? obj.fs : null;    /* Corresponding filesystem object */
            return res;
        }



        /*---------------------------------------------------------------------------

           Public Functions (FatFs API)

        ----------------------------------------------------------------------------*/



        /*-----------------------------------------------------------------------*/
        /* Mount/Unmount a Logical Drive                                         */
        /*-----------------------------------------------------------------------*/

        public FileResult MountDrive(
            ref FatFS fs,		/* Pointer to the filesystem object (NULL:unmount)*/
            string path,        /* Logical drive number to be mounted/unmounted */
            byte opt			/* Mode option 0:Do not mount (delayed mount), 1:Mount immediately */
        ) {
            FatFS cfs;
            int vol;
            FileResult res;
            byte[] rp;
            uint rpIndex = 0;

            /* Generate zer terminated byte array from filename */
            rp = path.ToNullTerminatedByteArray();

            /* Get logical drive number */
            vol = this.GetLogicalDriveNumber(rp, ref rpIndex);
            if (vol < 0) return FileResult.InvalidDrive;

            cfs = fatFs[vol];                   /* Pointer to fs object */

            if (cfs != null) {
                cfs.fs_type = 0;                /* Clear old fs object */
            }

            if (fs != null) {
                fs.fs_type = 0;                 /* Clear new fs object */
            }

            fatFs[vol] = fs;                    /* Register new fs object */

            if (opt == 0) return FileResult.Ok;         /* Do not mount now, it will be mounted later */

            res = this.FindVolume(ref rp, ref fs, 0);       /* Force mount the volume */

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Open or Create a File                                                 */
        /*-----------------------------------------------------------------------*/

        public FileResult OpenFile(
            ref FileObject fp,		/* Pointer to the blank file object */
            string fullFilename,    /* Pointer to the file name */
            byte mode		/* Access mode and file open mode flags */
        ) {
            FileResult res;
            var dj = new DirectoryObject();
            FatFS fs = null;
            byte[] path;
            uint pathIndex = 0;

            uint dw, cl, bcs, clst, sc;
            uint ofs;

            if (fp == null) return FileResult.InvalidObject;
            path = fullFilename.ToNullTerminatedByteArray();

            /* Get logical drive number */
            mode &= (byte)(FA_READ | FA_WRITE | FA_CREATE_ALWAYS | FA_CREATE_NEW | FA_OPEN_ALWAYS | FA_OPEN_APPEND);
            res = this.FindVolume(ref path, ref fs, mode);
            if (res == FileResult.Ok) {
                dj.obj.fs = fs;

                res = this.FollowFilePath(ref dj, path, ref pathIndex);   /* Follow the file path */

                if (res == FileResult.Ok) {
                    if ((dj.fn[NSFLAG] & NS_NONAME) > 0) {   /* Origin directory itself? */
                        res = FileResult.InvalidPathName;
                    }

                }
                /* Create or Open a file */
                if ((mode & (byte)(FA_CREATE_ALWAYS | FA_OPEN_ALWAYS | FA_CREATE_NEW)) > 0) {
                    if (res != FileResult.Ok) {
                        /* No file, create new */
                        if (res == FileResult.FileNotExist) {
                            /* There is no file to open, create a new entry */

                            res = this.RegisterDirectoryObject(ref dj);

                        }
                        mode |= FA_CREATE_ALWAYS;       /* File is created */
                    }
                    else {                               /* Any object with the same name is already existing */
                        if ((dj.obj.attr & (byte)(AM_RDO | AM_DIR)) > 0) {   /* Cannot overwrite it (R/O or DIR) */
                            res = FileResult.AccessDenied;
                        }
                        else {
                            if ((mode & FA_CREATE_NEW) > 0) res = FileResult.Exists;   /* Cannot create as new file */
                        }
                    }
                    if (res == FileResult.Ok && (mode & FA_CREATE_ALWAYS) > 0) {
                        /* Set directory entry initial state */
                        cl = this.LoadCluster(fs, fs.win.SubArray(dj.dirAsFsWinOffset));          /* Get current cluster chain */
                        StoreDword(ref fs.win, dj.dirAsFsWinOffset + DIR_CrtTime, this.GetFatTime());  /* Set created time */
                        fs.win[dj.dirAsFsWinOffset + DIR_Attr] = AM_ARC;          /* Reset attribute */
                        this.StoreCluster(ref fs, dj.dirAsFsWinOffset, 0);            /* Reset file allocation info */
                        StoreDword(ref fs.win, dj.dirAsFsWinOffset + DIR_FileSize, 0);
                        fs.wflag = 1;
                        if (cl != 0) {                       /* Remove the cluster chain if exist */
                            dw = fs.winsect;
                            res = this.RemoveChain(ref dj.obj, cl, 0);
                            if (res == FileResult.Ok) {
                                res = this.MoveWindow(ref fs, dw);
                                fs.last_clst = cl - 1;     /* Reuse the cluster hole */
                            }
                        }
                    }
                }
                else {   /* Open an existing file */
                    if (res == FileResult.Ok) {
                        /* Is the object existing? */
                        if ((dj.obj.attr & AM_DIR) > 0) {       /* File open against a directory */
                            res = FileResult.FileNotExist;
                        }
                        else {
                            if ((mode & FA_WRITE) > 0 && (dj.obj.attr & AM_RDO) > 0) { /* Write mode open against R/O file */
                                res = FileResult.AccessDenied;
                            }
                        }
                    }
                }
                if (res == FileResult.Ok) {
                    if ((mode & FA_CREATE_ALWAYS) > 0) mode |= FA_MODIFIED;   /* Set file change flag if created or overwritten */
                    fp.dir_sect = fs.winsect;         /* Pointer to the directory entry */
                    fp.dir_ptrAsFsWinOffset = dj.dirAsFsWinOffset;
                }


                if (res == FileResult.Ok) {
                    {
                        fp.obj.sclust = this.LoadCluster(fs, fs.win.SubArray(dj.dirAsFsWinOffset));                  /* Get object allocation info */
                        fp.obj.objsize = LoadDword(fs.win, dj.dirAsFsWinOffset + DIR_FileSize);
                    }
                    fp.obj.fs = fs;        /* Validate the file object */
                    fp.obj.id = fs.id;
                    fp.flag = mode;        /* Set file access mode */
                    fp.err = 0;            /* Clear error flag */
                    fp.sect = 0;           /* Invalidate current data sector */
                    fp.fptr = 0;           /* Set file pointer top of the file */


                    SetMemory(ref fp.buf, 0, FF_MAX_SS); /* Clear sector buffer */

                    if ((mode & FA_SEEKEND) > 0 && fp.obj.objsize > 0) {   /* Seek to end of file if FA_OPEN_APPEND is specified */
                        fp.fptr = fp.obj.objsize;         /* Offset to seek */
                        bcs = (uint)fs.csize * this.SS(fs);    /* Cluster size in byte */
                        clst = fp.obj.sclust;              /* Follow the cluster chain */
                        for (ofs = fp.obj.objsize; res == FileResult.Ok && ofs > bcs; ofs -= bcs) {
                            clst = this.GetFat(ref fp.obj, clst);
                            if (clst <= 1) res = FileResult.InternalError;
                            if (clst == 0xFFFFFFFF) res = FileResult.DiskError;
                        }
                        fp.clust = clst;
                        if (res == FileResult.Ok && (ofs % this.SS(fs)) > 0) {   /* Fill sector buffer if not on the sector boundary */
                            if ((sc = ClusterToSector(fs, clst)) == 0) {
                                res = FileResult.InternalError;
                            }
                            else {
                                fp.sect = sc + (uint)(ofs / this.SS(fs));

                                if (this.diskIO.DiskRead(fs.pdrv, ref fp.buf, fp.sect, 1) != DiskResult.Ok) res = FileResult.DiskError;

                            }
                        }
                    }
                }
            }

            if (res != FileResult.Ok) fp.obj.fs = null;   /* Invalidate file object on error */

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Read File                                                             */
        /*-----------------------------------------------------------------------*/

        public FileResult ReadFile(
            ref FileObject fp,    /* Pointer to the file object */
            ref byte[] buffer, /* Pointer to data buffer */
            uint totalBytesToRead,   /* Number of bytes to read */
            ref uint br    /* Pointer to number of bytes read */
        ) {
            FileResult res;
            FatFS fs = null;
            uint clst, sect;
            uint remain;
            uint rcnt, cc, csect;
            uint bufIndex = 0;


            br = 0;    /* Clear read byte counter */
            res = this.ValidateObject(ref fp.obj, ref fs);              /* Check validity of the file object */
            if (res != FileResult.Ok || (res = (FileResult)fp.err) != FileResult.Ok) return res;   /* Check validity */
            if ((fp.flag & FA_READ) == 0) return FileResult.AccessDenied; /* Check access mode */
            remain = fp.obj.objsize - fp.fptr;
            if (totalBytesToRead > remain) totalBytesToRead = remain;       /* Truncate btr by remaining bytes */

            for (; totalBytesToRead > 0;                             /* Repeat until btr bytes read */
                totalBytesToRead -= rcnt, br += rcnt, bufIndex += rcnt, fp.fptr += rcnt) {
                if (fp.fptr % this.SS(fs) == 0) {
                    /* On the sector boundary? */
                    csect = (uint)(fp.fptr / this.SS(fs) & (fs.csize - 1));    /* Sector offset in the cluster */
                    if (csect == 0) {
                        /* On the cluster boundary? */
                        if (fp.fptr == 0) {           /* On the top of the file? */
                            clst = fp.obj.sclust;      /* Follow cluster chain from the origin */
                        }
                        else {
                            /* Middle or end of the file */
                            clst = this.GetFat(ref fp.obj, fp.clust);    /* Follow cluster chain on the FAT */

                        }
                        if (clst < 2) {
                            fp.err = (byte)res;
                            return res;
                        }
                        if (clst == 0xFFFFFFFF) {
                            fp.err = (byte)FileResult.DiskError;
                            return res;
                        }
                        fp.clust = clst;               /* Update current cluster */
                    }
                    sect = ClusterToSector(fs, fp.clust);    /* Get current sector */
                    if (sect == 0) {
                        fp.err = (byte)FileResult.InternalError;
                        return res;
                    }
                    sect += csect;
                    cc = totalBytesToRead / this.SS(fs);                  /* When remaining bytes >= sector size, */
                    if (cc > 0) {
                        /* Read maximum contiguous sectors directly */
                        if (csect + cc > fs.csize) {   /* Clip at cluster boundary */
                            cc = fs.csize - csect;
                        }
                        var bytesToRead = cc * this.SS(fs);
                        var tempBuf = new byte[bytesToRead];
                        if (this.diskIO.DiskRead(fs.pdrv, ref tempBuf, sect, cc) != DiskResult.Ok) {
                            fp.err = (byte)FileResult.DiskError;
                            return res;
                        }
                        // HB: Copy buffer directly into result buffer
                        CopyMemory(ref buffer, (int)bufIndex, tempBuf, bytesToRead);

                        /* Replace one of the read sectors with cached data if it contains a dirty sector */
                        if ((fp.flag & FA_DIRTY) > 0 && fp.sect - sect < cc) {
                            CopyMemory(ref buffer, (int)((fp.sect - sect) * this.SS(fs)), fp.buf, this.SS(fs));
                        }

                        rcnt = this.SS(fs) * cc;             /* Number of bytes transferred */
                        continue;
                    }

                    if (fp.sect != sect) {
                        /* Load data sector if not in cache */
                        if ((fp.flag & FA_DIRTY) > 0) {
                            /* Write-back dirty sector cache */
                            if (this.diskIO.DiskWrite(fs.pdrv, fp.buf, fp.sect, 1) != DiskResult.Ok) {
                                fp.err = (byte)FileResult.DiskError;
                                return res;
                            }
                            fp.flag &= (byte)(~FA_DIRTY & 0xff);
                        }

                        /* Fill sector cache */
                        if (this.diskIO.DiskRead(fs.pdrv, ref fp.buf, sect, 1) != DiskResult.Ok) {
                            fp.err = (byte)FileResult.DiskError;
                            return res;
                        }
                    }
                    fp.sect = sect;
                }
                rcnt = this.SS(fs) - (uint)fp.fptr % this.SS(fs);    /* Number of bytes left in the sector */
                if (rcnt > totalBytesToRead) rcnt = totalBytesToRead;                 /* Clip it by btr if needed */

                CopyMemory(ref buffer, (int)bufIndex, fp.buf, (int)(fp.fptr % this.SS(fs)), rcnt);  /* Extract partial sector */

            }

            return FileResult.Ok;
        }



        /*-----------------------------------------------------------------------*/
        /* Write File                                                            */
        /*-----------------------------------------------------------------------*/

        public FileResult WriteFile(
            ref FileObject fp,			/* Pointer to the file object */
            byte[] buffer,   /* Pointer to the data to be written */
            uint bytesToWrite,          /* Number of bytes to write */
            ref uint bw			/* Pointer to number of bytes written */
        ) {

            FileResult res;
            FatFS fs = null;
            uint clst, sect;
            uint wcnt, cc, csect;
            uint buffIndex = 0;


            bw = 0; /* Clear write byte counter */
            res = this.ValidateObject(ref fp.obj, ref fs);           /* Check validity of the file object */
            if (res != FileResult.Ok || (res = (FileResult)fp.err) != FileResult.Ok) return res;    /* Check validity */
            if ((fp.flag & FA_WRITE) == 0) return FileResult.AccessDenied;  /* Check access mode */

            /* Check fptr wrap-around (file size cannot reach 4 GiB at FAT volume) */
            if ((fs.fs_type != FS_EXFAT) && (fp.fptr + bytesToWrite) < fp.fptr) {
                bytesToWrite = (uint)(0xFFFFFFFF - fp.fptr);
            }

            for (; bytesToWrite > 0;                            /* Repeat until all data written */
                bytesToWrite -= wcnt, bw += wcnt, buffIndex += wcnt, fp.fptr += wcnt, fp.obj.objsize = (fp.fptr > fp.obj.objsize) ? fp.fptr : fp.obj.objsize) {
                if (fp.fptr % this.SS(fs) == 0) {        /* On the sector boundary? */
                    csect = (uint)(fp.fptr / this.SS(fs)) & (fs.csize - 1);  /* Sector offset in the cluster */
                    if (csect == 0) {               /* On the cluster boundary? */
                        if (fp.fptr == 0) {
                            /* On the top of the file? */
                            clst = fp.obj.sclust;   /* Follow from the origin */
                            if (clst == 0) {
                                /* If no cluster is allocated, */
                                clst = this.CreateChain(ref fp.obj, 0);   /* create a new cluster chain */
                            }
                        }
                        else {
                            /* On the middle or end of the file */
                            clst = this.CreateChain(ref fp.obj, fp.clust);   /* Follow or stretch cluster chain on the FAT */
                        }
                        if (clst == 0) break;       /* Could not allocate a new cluster (disk full) */
                        if (clst == 1) {
                            fp.err = (byte)FileResult.InternalError;
                            return res;
                        }
                        if (clst == 0xFFFFFFFF) {
                            fp.err = (byte)FileResult.DiskError;
                            return res;
                        }
                        fp.clust = clst;            /* Update current cluster */
                        if (fp.obj.sclust == 0) fp.obj.sclust = clst;   /* Set start cluster if the first write */
                    }
                    if ((fp.flag & FA_DIRTY) > 0) {     /* Write-back sector cache */
                        if (this.diskIO.DiskWrite(fs.pdrv, fp.buf, fp.sect, 1) != DiskResult.Ok) {
                            fp.err = (byte)FileResult.DiskError;
                            return res;
                        }
                        fp.flag &= (byte)(~FA_DIRTY & 0xff);
                    }
                    sect = ClusterToSector(fs, fp.clust); /* Get current sector */
                    if (sect == 0) {
                        fp.err = (byte)FileResult.InternalError;
                        return res;
                    }
                    sect += csect;
                    cc = bytesToWrite / this.SS(fs);             /* When remaining bytes >= sector size, */
                    if (cc > 0) {
                        /* Write maximum contiguous sectors directly */
                        if (csect + cc > fs.csize) {    /* Clip at cluster boundary */
                            cc = fs.csize - csect;
                        }
                        var bytesToRead = cc * this.SS(fs);
                        var tempBuf = new byte[bytesToRead];
                        Array.Copy(buffer, (int)buffIndex, tempBuf, 0, (int)bytesToRead);
                        if (this.diskIO.DiskWrite(fs.pdrv, tempBuf, sect, cc) != DiskResult.Ok) {
                            fp.err = (byte)FileResult.DiskError;
                            return res;
                        }

                        if (fp.sect - sect < cc) {
                            /* Refill sector cache if it gets invalidated by the direct write */
                            CopyMemory(ref fp.buf, 0, buffer, (int)(buffIndex + ((fp.sect - sect) * this.SS(fs))), this.SS(fs));
                            fp.flag &= (byte)(~FA_DIRTY & 0xff);
                        }
                        wcnt = this.SS(fs) * cc;     /* Number of bytes transferred */
                        continue;
                    }
                    if (fp.sect != sect &&      /* Fill sector cache with file data */
                        fp.fptr < fp.obj.objsize &&
                        this.diskIO.DiskRead(fs.pdrv, ref fp.buf, sect, 1) != DiskResult.Ok) {
                        fp.err = (byte)FileResult.DiskError;
                        return res;
                    }
                    fp.sect = sect;
                }
                wcnt = this.SS(fs) - fp.fptr % this.SS(fs);   /* Number of bytes left in the sector */
                if (wcnt > bytesToWrite) wcnt = bytesToWrite;					/* Clip it by btw if needed */
                CopyMemory(ref fp.buf, (int)(fp.fptr % this.SS(fs)), buffer, (int)buffIndex, wcnt);  /* Fit data to the sector */
                fp.flag |= FA_DIRTY;
            }

            fp.flag |= FA_MODIFIED;             /* Set file change flag */

            return FileResult.Ok;
        }

        /*-----------------------------------------------------------------------*/
        /* Synchronize the File                                                  */
        /*-----------------------------------------------------------------------*/

        FileResult SyncFile(
            ref FileObject fp     /* Pointer to the file object */
        ) {
            FileResult res;
            FatFS fs = null;
            uint tm;
            uint dir_ptrAsFsWinOffset; // dir;


            res = this.ValidateObject(ref fp.obj, ref fs);  /* Check validity of the file object */
            if (res == FileResult.Ok) {
                if ((fp.flag & FA_MODIFIED) > 0) {
                    /* Is there any change to the file? */

                    if ((fp.flag & FA_DIRTY) > 0) {   /* Write-back cached data if needed */
                        if (this.diskIO.DiskWrite(fs.pdrv, fp.buf, fp.sect, 1) != DiskResult.Ok) return FileResult.DiskError;
                        fp.flag &= (byte)(~FA_DIRTY & 0xff);
                    }

                    /* Update the directory entry */
                    tm = this.GetFatTime();             /* Modified time */

                    {
                        res = this.MoveWindow(ref fs, fp.dir_sect);
                        if (res == FileResult.Ok) {
                            dir_ptrAsFsWinOffset = fp.dir_ptrAsFsWinOffset;
                            fs.win[dir_ptrAsFsWinOffset + DIR_Attr] |= AM_ARC;                        /* Set archive attribute to indicate that the file has been changed */
                            this.StoreCluster(ref fp.obj.fs, dir_ptrAsFsWinOffset, fp.obj.sclust);      /* Update file allocation information  */
                            StoreDword(ref fs.win, dir_ptrAsFsWinOffset + DIR_FileSize, fp.obj.objsize);   /* Update file size */
                            StoreDword(ref fs.win, dir_ptrAsFsWinOffset + DIR_ModTime, tm);                /* Update modified time */
                            StoreWord(ref fs.win, dir_ptrAsFsWinOffset + DIR_LstAccDate, 0);
                            fs.wflag = 1;
                            res = this.SyncFileSystem(ref fs);                  /* Restore it to the directory */
                            fp.flag &= (byte)(~FA_MODIFIED & 0xff);
                        }
                    }
                }
            }

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Close File                                                            */
        /*-----------------------------------------------------------------------*/

        public FileResult CloseFile(
            ref FileObject fp     /* Pointer to the file object to be closed */
        ) {
            FileResult res;
            FatFS fs = null;


            res = this.SyncFile(ref fp);                   /* Flush cached data */
            if (res == FileResult.Ok) {
                res = this.ValidateObject(ref fp.obj, ref fs);  /* Lock volume */
                if (res == FileResult.Ok) {

                    fp.obj.fs = null; /* Invalidate file object */


                }
            }
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Seek File Read/Write Pointer                                          */
        /*-----------------------------------------------------------------------*/

        FileResult SeekFile(
            ref FileObject fp,        /* Pointer to the file object */
            ref uint ofs     /* File pointer from top of file */
        ) {
            FileResult res;
            FatFS fs = null;
            uint clst, bcs, nsect;
            uint ifptr;


            res = this.ValidateObject(ref fp.obj, ref fs);      /* Check validity of the file object */
            if (res == FileResult.Ok) res = (FileResult)fp.err;

            if (res != FileResult.Ok) return res;

            /* Normal Seek */
            {
                if (ofs > fp.obj.objsize && (fp.flag & FA_WRITE) == 0) {   /* In read-only mode, clip offset with the file size */
                    ofs = fp.obj.objsize;
                }
                ifptr = fp.fptr;
                fp.fptr = nsect = 0;
                if (ofs > 0) {
                    bcs = (uint)fs.csize * this.SS(fs);    /* Cluster size (byte) */
                    if (ifptr > 0 &&
                        (ofs - 1) / bcs >= (ifptr - 1) / bcs) {   /* When seek to same or following cluster, */
                        fp.fptr = (ifptr - 1) & ~(uint)(bcs - 1);   /* start from the current cluster */
                        ofs -= fp.fptr;
                        clst = fp.clust;
                    }
                    else {
                        /* When seek to back cluster, */
                        clst = fp.obj.sclust;                  /* start from the first cluster */

                        if (clst == 0) {                       /* If no cluster chain, create a new chain */
                            clst = this.CreateChain(ref fp.obj, 0);
                            if (clst == 1) {
                                fp.err = (byte)FileResult.InternalError;
                                return res;
                            }
                            if (clst == 0xFFFFFFFF) {
                                fp.err = (byte)FileResult.DiskError;
                                return res;
                            }
                            fp.obj.sclust = clst;
                        }

                        fp.clust = clst;
                    }
                    if (clst != 0) {
                        while (ofs > bcs) {                       /* Cluster following loop */
                            ofs -= bcs; fp.fptr += bcs;

                            if ((fp.flag & FA_WRITE) > 0) {           /* Check if in write mode or not */
                                if (FF_FS_EXFAT > 0 && fp.fptr > fp.obj.objsize) {
                                    /* No FAT chain object needs correct objsize to generate FAT value */
                                    fp.obj.objsize = fp.fptr;
                                    fp.flag |= FA_MODIFIED;
                                }
                                clst = this.CreateChain(ref fp.obj, clst);    /* Follow chain with forceed stretch */
                                if (clst == 0) {               /* Clip file size in case of disk full */
                                    ofs = 0; break;
                                }
                            }
                            else {
                                clst = this.GetFat(ref fp.obj, clst); /* Follow cluster chain if not in write mode */
                            }
                            if (clst == 0xFFFFFFFF) {
                                fp.err = (byte)FileResult.DiskError;
                                return res;
                            }
                            if (clst <= 1 || clst >= fs.n_fatent) {
                                fp.err = (byte)FileResult.InternalError;
                                return res;
                            }
                            fp.clust = clst;
                        }
                        fp.fptr += ofs;
                        if ((ofs % this.SS(fs)) > 0) {
                            nsect = ClusterToSector(fs, clst);    /* Current sector */
                            if (nsect == 0) {
                                fp.err = (byte)FileResult.InternalError;
                                return res;
                            }
                            nsect += (uint)(ofs / this.SS(fs));
                        }
                    }
                }
                if (fp.fptr > fp.obj.objsize) {   /* Set file change flag if the file size is extended */
                    fp.obj.objsize = fp.fptr;
                    fp.flag |= FA_MODIFIED;
                }
                if ((fp.fptr % this.SS(fs)) > 0 && nsect != fp.sect) {   /* Fill sector cache if needed */

                    if ((fp.flag & FA_DIRTY) > 0) {           /* Write-back dirty sector cache */
                        if (this.diskIO.DiskWrite(fs.pdrv, fp.buf, fp.sect, 1) != DiskResult.Ok) {
                            fp.err = (byte)FileResult.DiskError;
                            return res;
                        }
                        fp.flag &= (byte)(~FA_DIRTY & 0xff);
                    }

                    if (this.diskIO.DiskRead(fs.pdrv, ref fp.buf, nsect, 1) != DiskResult.Ok) /* Fill sector cache */
                    {
                        fp.err = (byte)FileResult.DiskError;
                        return res;
                    }

                    fp.sect = nsect;
                }
            }

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Create a Directory Object                                             */
        /*-----------------------------------------------------------------------*/

        public FileResult OpenDirectory(
            ref DirectoryObject dp,			/* Pointer to directory object to create */
            byte[] path	/* Pointer to the directory path */

        ) {

            FileResult res;
            FatFS fs = null;
            uint pathIndex = 0;

            if (dp == null) return FileResult.InvalidObject;

            /* Get logical drive */
            res = this.FindVolume(ref path, ref fs, 0);
            if (res == FileResult.Ok) {
                dp.obj.fs = fs;
                res = this.FollowFilePath(ref dp, path, ref pathIndex);          /* Follow the path to the directory */
                if (res == FileResult.Ok) {                     /* Follow completed */
                    if ((dp.fn[NSFLAG] & NS_NONAME) == 0) { /* It is not the origin directory itself */
                        if ((dp.obj.attr & AM_DIR) > 0) {       /* This object is a sub-directory */
                            {
                                dp.obj.sclust = this.LoadCluster(fs, fs.win, dp.dirAsFsWinOffset); /* Get object allocation info */
                            }
                        }
                        else {
                            /* This object is a file */
                            res = FileResult.PathNotFound;
                        }
                    }
                    if (res == FileResult.Ok) {
                        dp.obj.id = fs.id;
                        res = this.SetDirectoryIndex(ref dp, 0);         /* Rewind directory */
                    }
                }
                if (res == FileResult.FileNotExist) res = FileResult.PathNotFound;
            }
            if (res != FileResult.Ok) dp.obj.fs = null;     /* Invalidate the directory object if function faild */

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Close Directory                                                       */
        /*-----------------------------------------------------------------------*/

        public FileResult CloseDirectory(
            ref DirectoryObject dp     /* Pointer to the directory object to be closed */
        ) {
            FileResult res;
            FatFS fs = null;


            res = this.ValidateObject(ref dp.obj, ref fs);  /* Check validity of the file object */
            if (res == FileResult.Ok) {

                dp.obj.fs = null; /* Invalidate directory object */

            }
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Read Directory Entries in Sequence                                    */
        /*-----------------------------------------------------------------------*/

        public FileResult ReadDirectoryEntry(
            ref DirectoryObject dp,            /* Pointer to the open directory object */
            ref FileInfo fno        /* Pointer to file information to return */
        ) {
            FileResult res;
            FatFS fs = null;

            res = this.ValidateObject(ref dp.obj, ref fs);  /* Check validity of the directory object */
            if (res == FileResult.Ok) {
                if (fno == null) {
                    res = this.SetDirectoryIndex(ref dp, 0);           /* Rewind the directory object */
                }
                else {

                    res = this.ReadFileInDirectory(ref dp);        /* Read an item */
                    if (res == FileResult.FileNotExist) res = FileResult.Ok; /* Ignore end of directory */
                    if (res == FileResult.Ok) {
                        /* A valid entry is found */
                        this.GetFileInfo(dp, ref fno);      /* Get the object information */
                        res = this.NextDirectory(ref dp, 0);      /* Increment index for next */
                        if (res == FileResult.FileNotExist) res = FileResult.Ok; /* Ignore end of directory now */
                    }

                }
            }
            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Get File Status                                                       */
        /*-----------------------------------------------------------------------*/

        public FileResult GetFileAttributes(
            string fullFilename,  /* Pointer to the file path */
            ref FileInfo fno		/* Pointer to file information to return */
        ) {
            FileResult res;
            var dj = new DirectoryObject();
            byte[] path;
            uint pathIndex = 0;

            path = fullFilename.ToNullTerminatedByteArray();

            /* Get logical drive */
            res = this.FindVolume(ref path, ref dj.obj.fs, 0);
            if (res == FileResult.Ok) {
                res = this.FollowFilePath(ref dj, path, ref pathIndex);  /* Follow the file path */
                if (res == FileResult.Ok) {             /* Follow completed */
                    if ((dj.fn[NSFLAG] & NS_NONAME) > 0) {  /* It is origin directory */
                        res = FileResult.InvalidPathName;
                    }
                    else {
                        /* Found an object */
                        if (fno != null) this.GetFileInfo(dj, ref fno);
                    }
                }
            }

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Get Number of Free Clusters                                           */
        /*-----------------------------------------------------------------------*/

        public FileResult GetFreeSpace(
            string driveNum,  /* Logical drive number */
            ref uint nclst,     /* Pointer to a variable to return number of free clusters */
            ref FatFS fatfs		/* Pointer to return pointer to corresponding filesystem object */
        ) {

            FileResult res;
            FatFS fs = null;
            uint nfree, clst, sect, stat;
            uint i;
            var obj = new FileObjectIdentifier();

            var path = driveNum.ToNullTerminatedByteArray();

            /* Get logical drive */
            res = this.FindVolume(ref path, ref fs, 0);
            if (res == FileResult.Ok) {
                fatfs = fs;             /* Return ptr to the fs object */
                /* If free_clst is valid, return it without full FAT scan */
                if (fs.free_clst <= fs.n_fatent - 2) {
                    nclst = fs.free_clst;
                }
                else {
                    /* Scan FAT to obtain number of free clusters */
                    nfree = 0;
                    if (fs.fs_type == FS_FAT12) {   /* FAT12: Scan bit field FAT entries */
                        clst = 2; obj.fs = fs;
                        do {
                            stat = this.GetFat(ref obj, clst);
                            if (stat == 0xFFFFFFFF) { res = FileResult.DiskError; break; }
                            if (stat == 1) { res = FileResult.InternalError; break; }
                            if (stat == 0) nfree++;
                        } while (++clst < fs.n_fatent);
                    }
                    else {
                        {   /* FAT16/32: Scan WORD/DWORD FAT entries */
                            clst = fs.n_fatent; /* Number of entries */
                            sect = fs.fatbase;      /* Top of the FAT */
                            i = 0;                  /* Offset in the sector */
                            do {    /* Counts numbuer of entries with zero in the FAT */
                                if (i == 0) {
                                    res = this.MoveWindow(ref fs, sect++);
                                    if (res != FileResult.Ok) break;
                                }
                                if (fs.fs_type == FS_FAT16) {
                                    if (LoadWord(fs.win, i) == 0) nfree++;
                                    i += 2;
                                }
                                else {
                                    if ((LoadDword(fs.win, i) & 0x0FFFFFFF) == 0) nfree++;
                                    i += 4;
                                }
                                i %= this.SS(fs);
                            } while (--clst > 0);
                        }
                    }

                    nclst = nfree;            /* Return the free clusters */
                    fs.free_clst = nfree;   /* Now free_clst is valid */
                    fs.fsi_flag |= 1;       /* FAT32: FSInfo is to be updated */
                }
            }

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Truncate File                                                         */
        /*-----------------------------------------------------------------------*/

        FileResult TruncateFile(
            ref FileObject fp     /* Pointer to the file object */
        ) {
            FileResult res;
            FatFS fs = null;
            uint ncl;


            res = this.ValidateObject(ref fp.obj, ref fs);  /* Check validity of the file object */
            if (res != FileResult.Ok || (res = (FileResult)fp.err) != FileResult.Ok) return res;
            if ((fp.flag & FA_WRITE) == 0) return FileResult.AccessDenied;    /* Check access mode */

            if (fp.fptr < fp.obj.objsize) {   /* Process when fptr is not on the eof */
                if (fp.fptr == 0) {   /* When set file size to zero, remove entire cluster chain */
                    res = this.RemoveChain(ref fp.obj, fp.obj.sclust, 0);
                    fp.obj.sclust = 0;
                }
                else {
                    /* When truncate a part of the file, remove remaining clusters */
                    ncl = this.GetFat(ref fp.obj, fp.clust);
                    res = FileResult.Ok;
                    if (ncl == 0xFFFFFFFF) res = FileResult.DiskError;
                    if (ncl == 1) res = FileResult.InternalError;
                    if (res == FileResult.Ok && ncl < fs.n_fatent) {
                        res = this.RemoveChain(ref fp.obj, ncl, fp.clust);
                    }
                }
                fp.obj.objsize = fp.fptr; /* Set file size to current read/write point */
                fp.flag |= FA_MODIFIED;

                if (res == FileResult.Ok && (fp.flag & FA_DIRTY) > 0) {
                    if (this.diskIO.DiskWrite(fs.pdrv, fp.buf, fp.sect, 1) != DiskResult.Ok) {
                        res = FileResult.DiskError;
                    }
                    else {
                        fp.flag &= (byte)(~FA_DIRTY & 0xff);
                    }
                }

                if (res != FileResult.Ok) {
                    fp.err = (byte)res;
                    return res;
                }
            }

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Delete a File/Directory                                               */
        /*-----------------------------------------------------------------------*/

        public FileResult DeleteFileOrDirectory(
            string fullFilename		/* Pointer to the file or directory path */
        ) {

            FileResult res;
            var dj = new DirectoryObject();
            var sdj = new DirectoryObject();
            uint dclst = 0;
            FatFS fs = null;
            byte[] path;
            uint pathIndex = 0;

            path = fullFilename.ToNullTerminatedByteArray();

            /* Get logical drive */
            res = this.FindVolume(ref path, ref fs, FA_WRITE);
            if (res == FileResult.Ok) {
                dj.obj.fs = fs;
                res = this.FollowFilePath(ref dj, path, ref pathIndex);      /* Follow the file path */
                if (FF_FS_RPATH > 0 && res == FileResult.Ok && (dj.fn[NSFLAG] & NS_DOT) > 0) {
                    res = FileResult.InvalidPathName;           /* Cannot remove dot entry */
                }

                if (res == FileResult.Ok) {                 /* The object is accessible */
                    if ((dj.fn[NSFLAG] & NS_NONAME) > 0) {
                        res = FileResult.InvalidPathName;       /* Cannot remove the origin directory */
                    }
                    else {
                        if ((dj.obj.attr & AM_RDO) > 0) {
                            res = FileResult.AccessDenied;      /* Cannot remove R/O object */
                        }
                    }
                    if (res == FileResult.Ok) {

                        dclst = this.LoadCluster(fs, fs.win, dj.dirAsFsWinOffset);

                        if ((dj.obj.attr & AM_DIR) > 0) {           /* Is it a sub-directory? */

                            {
                                sdj.obj.fs = fs;                /* Open the sub-directory */
                                sdj.obj.sclust = dclst;
                                res = this.SetDirectoryIndex(ref sdj, 0);
                                if (res == FileResult.Ok) {
                                    res = this.ReadFileInDirectory(ref sdj);         /* Test if the directory is empty */
                                    if (res == FileResult.Ok) res = FileResult.AccessDenied;    /* Not empty? */
                                    if (res == FileResult.FileNotExist) res = FileResult.Ok;    /* Empty? */
                                }
                            }
                        }
                    }
                    if (res == FileResult.Ok) {
                        res = this.RemoveFromDirectory(ref dj);          /* Remove the directory entry */
                        if (res == FileResult.Ok && dclst != 0) {   /* Remove the cluster chain if exist */

                            res = this.RemoveChain(ref dj.obj, dclst, 0);

                        }
                        if (res == FileResult.Ok) res = this.SyncFileSystem(ref fs);
                    }
                }

            }

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Create a Directory                                                    */
        /*-----------------------------------------------------------------------*/

        public FileResult CreateDirectory(

            string fullFilename		/* Pointer to the directory path */
        ) {

            FileResult res;
            var dj = new DirectoryObject();
            FatFS fs = null;
            uint dirAsFsWinOffset;
            uint dcl, pcl, tm;
            byte[] path;
            uint pathIndex = 0;

            path = fullFilename.ToNullTerminatedByteArray();

            /* Get logical drive */
            res = this.FindVolume(ref path, ref fs, FA_WRITE);
            if (res == FileResult.Ok) {
                dj.obj.fs = fs;
                res = this.FollowFilePath(ref dj, path, ref pathIndex);          /* Follow the file path */
                if (res == FileResult.Ok) res = FileResult.Exists;      /* Any object with same name is already existing */
                if (FF_FS_RPATH > 0 && res == FileResult.FileNotExist && (dj.fn[NSFLAG] & NS_DOT) > 0) {
                    res = FileResult.InvalidPathName;
                }
                if (res == FileResult.FileNotExist) {               /* Can create a new directory */
                    dcl = this.CreateChain(ref dj.obj, 0);     /* Allocate a cluster for the new directory table */
                    dj.obj.objsize = (uint)fs.csize * this.SS(fs);
                    res = FileResult.Ok;
                    if (dcl == 0) res = FileResult.AccessDenied;        /* No space to allocate a new cluster */
                    if (dcl == 1) res = FileResult.InternalError;
                    if (dcl == 0xFFFFFFFF) res = FileResult.DiskError;
                    if (res == FileResult.Ok) res = this.SyncWindow(ref fs);    /* Flush FAT */
                    tm = this.GetFatTime();
                    if (res == FileResult.Ok) {
                        /* Initialize the new directory table */
                        res = this.ClearDirectory(ref fs, dcl);      /* Clean up the new table */
                        if (res == FileResult.Ok && (FF_FS_EXFAT == 0 || fs.fs_type != FS_EXFAT)) {
                            /* Create dot entries (FAT only) */
                            dirAsFsWinOffset = 0;
                            SetMemory(ref fs.win, (int)(dirAsFsWinOffset + DIR_Name), ' ', 11);   /* Create "." entry */
                            fs.win[dirAsFsWinOffset + DIR_Name] = (byte)'.';
                            fs.win[dirAsFsWinOffset + DIR_Attr] = AM_DIR;
                            StoreDword(ref fs.win, dirAsFsWinOffset + DIR_ModTime, tm);
                            this.StoreCluster(ref fs, dirAsFsWinOffset, dcl);
                            CopyMemory(ref fs.win, (int)(dirAsFsWinOffset + SZDIRE), fs.win, (int)dirAsFsWinOffset, SZDIRE); /* Create ".." entry */
                            fs.win[dirAsFsWinOffset + SZDIRE + 1] = (byte)'.';
                            pcl = dj.obj.sclust;
                            this.StoreCluster(ref fs, dirAsFsWinOffset + SZDIRE, pcl);
                            fs.wflag = 1;
                        }
                    }
                    if (res == FileResult.Ok) {
                        res = this.RegisterDirectoryObject(ref dj);  /* Register the object to the directoy */
                    }
                    if (res == FileResult.Ok) {

                        dirAsFsWinOffset = dj.dirAsFsWinOffset;
                        StoreDword(ref fs.win, dirAsFsWinOffset + DIR_ModTime, tm);    /* Created time */
                        this.StoreCluster(ref fs, dirAsFsWinOffset, dcl);             /* Table start cluster */
                        fs.win[dirAsFsWinOffset + DIR_Attr] = AM_DIR;               /* Attribute */
                        fs.wflag = 1;

                        if (res == FileResult.Ok) {
                            res = this.SyncFileSystem(ref fs);
                        }
                    }
                    else {
                        this.RemoveChain(ref dj.obj, dcl, 0);        /* Could not register, remove cluster chain */
                    }
                }

            }

            return res;
        }

        /*-----------------------------------------------------------------------*/
        /* Rename a File/Directory                                               */
        /*-----------------------------------------------------------------------*/

        public FileResult RenameFileOrDirectory(
            string oldFullFilename,	/* Pointer to the object name to be renamed */
            string newFullFilename	/* Pointer to the new name */
        ) {

            FileResult res;
            var djo = new DirectoryObject();
            var djn = new DirectoryObject();
            FatFS fs = null;
            var buf = new byte[SZDIRE];
            uint dw;
            byte[] path_old;
            byte[] path_new;
            uint pathOldIndex = 0;
            uint pathNewIndex = 0;

            uint dirAsFsWinOffset;

            path_old = oldFullFilename.ToNullTerminatedByteArray();
            path_new = newFullFilename.ToNullTerminatedByteArray();

            this.GetLogicalDriveNumber(path_new, ref pathNewIndex);                        /* Snip the drive number of new name off */
            res = this.FindVolume(ref path_old, ref fs, FA_WRITE);   /* Get logical drive of the old object */
            if (res == FileResult.Ok) {
                djo.obj.fs = fs;
                res = this.FollowFilePath(ref djo, path_old, ref pathOldIndex);      /* Check old object */
                if (res == FileResult.Ok && (djo.fn[NSFLAG] & (NS_DOT | NS_NONAME)) > 0) res = FileResult.InvalidPathName;  /* Check validity of name */

                if (res == FileResult.Ok) {                     /* Object to be renamed is found */

                    {
                        /* At FAT/FAT32 volume */
                        CopyMemory(ref buf, 0, fs.win, (int)djo.dirAsFsWinOffset, SZDIRE);          /* Save directory entry of the object */
                        djn = djo.Clone(fs);
                        res = this.FollowFilePath(ref djn, path_new, ref pathNewIndex);      /* Make sure if new object name is not in use */
                        if (res == FileResult.Ok) {
                            /* Is new name already in use by any other object? */
                            res = (djn.obj.sclust == djo.obj.sclust && djn.dptr == djo.dptr) ? FileResult.FileNotExist : FileResult.Exists;
                        }
                        if (res == FileResult.FileNotExist) {               /* It is a valid path and no name collision */
                            res = this.RegisterDirectoryObject(ref djn);         /* Register the new entry */
                            if (res == FileResult.Ok) {
                                dirAsFsWinOffset = djn.dirAsFsWinOffset;                    /* Copy directory entry of the object except name */
                                CopyMemory(ref fs.win, (int)(dirAsFsWinOffset + 13), buf, 13, SZDIRE - 13);
                                fs.win[dirAsFsWinOffset + DIR_Attr] = buf[DIR_Attr];
                                if ((fs.win[dirAsFsWinOffset + DIR_Attr] & AM_DIR) == 0) fs.win[dirAsFsWinOffset + DIR_Attr] |= AM_ARC; /* Set archive attribute if it is a file */
                                fs.wflag = 1;
                                if ((fs.win[dirAsFsWinOffset + DIR_Attr] & AM_DIR) > 0 && djo.obj.sclust != djn.obj.sclust) {   /* Update .. entry in the sub-directory if needed */
                                    dw = ClusterToSector(fs, this.LoadCluster(fs, fs.win, dirAsFsWinOffset));
                                    if (dw == 0) {
                                        res = FileResult.InternalError;
                                    }
                                    else {
                                        /* Start of critical section where an interruption can cause a cross-link */
                                        res = this.MoveWindow(ref fs, dw);
                                        dirAsFsWinOffset = SZDIRE * 1;  /* Ptr to .. entry */
                                        if (res == FileResult.Ok && fs.win[dirAsFsWinOffset + 1] == '.') {
                                            this.StoreCluster(ref fs, dirAsFsWinOffset, djn.obj.sclust);
                                            fs.wflag = 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (res == FileResult.Ok) {
                        res = this.RemoveFromDirectory(ref djo);     /* Remove old entry */
                        if (res == FileResult.Ok) {
                            res = this.SyncFileSystem(ref fs);
                        }
                    }
                }
            }
            return res;
        }

      
    }
}

