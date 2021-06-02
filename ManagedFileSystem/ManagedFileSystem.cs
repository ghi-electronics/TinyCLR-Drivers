using System;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.ManagedFileSystem {
    public enum FileMode {
        OpenExisting = 0x00,
        Read = 0x01,
        Write = 0x02,
        CreateNew = 0x04,
        CreateAlways = 0x08,
        OpenAlways = 0x10,
        Append = 0x30,
    }

    public enum FileAttributes : uint {
        ReadOnly = 0x1,
        Hidden = 0x2,
        System = 0x4,
        Directory = 0x10,
        Archive = 0x20,
        Normal = 0x80,
        NotExists = 0xFFFFFFFF
    }
    public class ManagedFileSystem {

        FATFileSystem current;
        FATFileSystem.FatFS fs = null;
        private FATFileSystem Current => this.current;
        private FATFileSystem.FatFS Fs => this.fs;

        private static bool mounted = false;


        public ManagedFileSystem(SpiController spiController, GpioPin chipselectPin, uint speed = 4_000_000) {
            this.fs = new FATFileSystem.FatFS();
            this.current = new FATFileSystem(new SdSpiDriver(spiController, chipselectPin, speed));
        }

        public void Mount() {
            if (mounted)
                throw new Exception("Support one drive only.");

            var res = this.current.MountDrive(ref this.fs, "", 1);
            if (res == FATFileSystem.FileResult.Ok)
                mounted = true;

        }

        public void Unmount() => mounted = false;

        public string Root => @"Z:\";

        public string DriveFormat {
            get {
                switch (this.fs.fs_type) {
                    case FATFileSystem.FS_EXFAT:
                        return "EXFAT";
                    case FATFileSystem.FS_FAT12:
                        return "FAT12";
                    case FATFileSystem.FS_FAT16:
                        return "FAT16";
                    case FATFileSystem.FS_FAT32:
                        return "FAT32";
                }
                return "";
            }
        }

        public bool IsReady => mounted;

        public long AvailableFreeSpace {
            get {
                uint fre_clust = 0;
                uint fre_sect;

                /* Get volume information and free clusters of drive 1 */
                var res = this.current.GetFreeSpace("0:", ref fre_clust, ref this.fs);
                if (res != FATFileSystem.FileResult.Ok) {
                    throw new Exception($"An error occured. {res.ToString()}");
                };

                /* Get total sectors and free sectors */
                //tot_sect = (fs.n_fatent - 2) * fs.csize;
                fre_sect = fre_clust * this.fs.csize;
                return fre_sect; //KB = fre_sect/2 
            }
        }

        public long TotalFreeSpace {
            get {
                uint fre_clust = 0;
                uint fre_sect;

                /* Get volume information and free clusters of drive 1 */
                var res = this.current.GetFreeSpace("0:", ref fre_clust, ref this.fs);
                if (res != FATFileSystem.FileResult.Ok) {
                    throw new Exception($"An error occured. {res.ToString()}");
                };

                /* Get total sectors and free sectors */
                //tot_sect = (fs.n_fatent - 2) * fs.csize;
                fre_sect = fre_clust * this.fs.csize;
                return fre_sect; //KB = fre_sect/2 
            }
        }

        public long TotalSize {
            get {
                uint fre_clust = 0;
                uint tot_sect;

                /* Get volume information and free clusters of drive 1 */
                var res = this.current.GetFreeSpace("0:", ref fre_clust, ref this.fs);
                if (res != FATFileSystem.FileResult.Ok) {
                    throw new Exception($"An error occured. {res.ToString()}");
                };

                /* Get total sectors and free sectors */
                tot_sect = (this.fs.n_fatent - 2) * this.fs.csize;
                //fre_sect = fre_clust * fs.csize;
                return tot_sect; //KB = tot_sect/2 
            }
        }

        public string VolumeLabel {
            get {

                var path = "/";
                var fno = new FATFileSystem.FileInfo();
                var dir = new FATFileSystem.DirectoryObject();
                var buff = path.ToNullTerminatedByteArray();

                var res = this.current.OpenDirectory(ref dir, buff);                      /* Open the directory */
                if (res == FATFileSystem.FileResult.Ok) {
                    res = this.current.ReadDirectoryEntry(ref dir, ref fno);           /* Read a directory item */

                    if (res == FATFileSystem.FileResult.Ok) {
                        return fno.fileName.ToStringNullTerminationRemoved();
                    }
                }
                return string.Empty;
            }
        }

        public string Name { get; set; } = "SpiSD";

        public void CreateDirectory(string path) {
            path = this.ReformatPath(path);
            var res = this.current.CreateDirectory(path);
            if (res != FATFileSystem.FileResult.Exists)
                res.ThrowIfError();
        }
        public void Delete(string path) {
            path = this.ReformatPath(path);
            var res = this.current.DeleteFileOrDirectory(path);     /* Give a work area to the default drive */
            res.ThrowIfError();

        }

        public FileHandle OpenFile(string path, FileMode mode) {
            path = this.ReformatPath(path);

            var fileObject = new FATFileSystem.FileObject();
            var fno = new FATFileSystem.FileInfo();

            var res = this.Current.OpenFile(ref fileObject, path, (byte)mode);

            if (res == FATFileSystem.FileResult.Ok) {
                return new FileHandle(fileObject, fno, path);
            }
            else
                res.ThrowIfError();

            return null;

        }

        public int ReadFile(FileHandle file, byte[] data, int offset, uint count) {
            uint bw = 0;

            var res = this.Current.ReadFile(ref file.fileObject, ref data, count, ref bw);

            if (res == FATFileSystem.FileResult.Ok) {
                return (int)bw;
            }

            return -1;
        }

        public void WriteFile(FileHandle file, byte[] data, int offset, uint count) {
            uint bw = 0;
            var res = this.Current.WriteFile(ref file.fileObject, data, count, ref bw);    /* Write data to the file */
            res.ThrowIfError();
        }

        public void FlushFile(FileHandle file) => this.Current.SyncFile(ref file.fileObject);
        public void CloseFile(FileHandle file) => this.Current.CloseFile(ref file.fileObject);
        public int SeekFile(FileHandle file, int offset) {
            var p = (uint)offset;
            this.Current.SeekFile(ref file.fileObject, ref p);

            return (int)p;
        }

        public void Rename(string fullPath1, string fullPath2) {
            var path1 = this.ReformatPath(fullPath1);
            var path2 = this.ReformatPath(fullPath2);

            this.Current.RenameFileOrDirectory(path1, path2);
        }

        public bool FileExist(string path) {
            var attribute = this.GetAttributes(path);

            return attribute != FileAttributes.NotExists;
        }

        public bool DirectoryExist(string path) {
            var attribute = this.GetAttributes(path);

            return attribute == FileAttributes.Directory;
        }

        public FileAttributes GetAttributes(string path) {
            path = this.ReformatPath(path);

            var fno = new FATFileSystem.FileInfo();

            var res = this.Current.GetFileAttributes(path, ref fno);

            switch (res) {

                case FATFileSystem.FileResult.Ok:
                    if ((fno.fileAttribute & FATFileSystem.AM_DIR) > 0) return FileAttributes.Directory;
                    if ((fno.fileAttribute & FATFileSystem.AM_RDO) > 0) return FileAttributes.ReadOnly;
                    if ((fno.fileAttribute & FATFileSystem.AM_HID) > 0) return FileAttributes.Hidden;
                    if ((fno.fileAttribute & FATFileSystem.AM_SYS) > 0) return FileAttributes.System;
                    if ((fno.fileAttribute & FATFileSystem.AM_ARC) > 0) return FileAttributes.Archive;

                    break;

                case FATFileSystem.FileResult.FileNotExist:
                    return FileAttributes.NotExists;


                default:
                    throw new Exception("error:" + res);
            }
            return FileAttributes.Normal;
        }

        private string ReformatPath(string sPath) {
            if (sPath.IndexOf(this.Root) == 0) {
                sPath = sPath.Substring(3, sPath.Length - 3);
            }


            if (sPath.IndexOf(@"/") != 0 && sPath.IndexOf(@"\") != 0) {
                sPath = "/" + sPath;
            }

            return sPath;
        }


    }

    public class FileHandle {
        public FileHandle(FATFileSystem.FileObject fileObject, FATFileSystem.FileInfo fno, string filename) {
            this.fileObject = fileObject;
            this.fno = fno;
            this.FileSize = fno.fileSize;
            this.FileName = filename;
            this.Attribute = fno.fileAttribute;
            this.Time = fno.fileTime;
            this.Date = fno.fileDate;
        }

        public FATFileSystem.FileObject fileObject;
        public FATFileSystem.FileInfo fno;
        public long FileSize { get; }
        public string FileName { get; }
        public uint Date { get; }
        public uint Time { get; }
        public uint Attribute { get; }
    }


}
