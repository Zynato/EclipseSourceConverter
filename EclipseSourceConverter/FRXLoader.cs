using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    static class FRXLoader
    {
        public static byte[] LoadFormResource(string frxPath, int pos) {
            // Based on: http://stackoverflow.com/q/27988820
            using (BinaryReader reader = new BinaryReader(File.Open(frxPath, FileMode.Open))) {
                reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                int length = reader.ReadInt32();
                return reader.ReadBytes(length);
            }
        }
    }
}
