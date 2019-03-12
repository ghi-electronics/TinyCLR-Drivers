using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace GHIElectronics.TinyCLR.Drivers.STMicroelectronics.SPWF04Sx
{
    public class FileEntity {

        public string Name { get; private set; }
        public string Volume { get; private set; }
        public int Length { get; private set; }

        public FileEntity()
        { }
        public FileEntity(string volume, string length, string name)
        {
            this.Name = name;
            this.Volume = volume;
            this.Length = int.Parse(length);
        }
    }
}
