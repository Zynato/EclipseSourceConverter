using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    class Announcer
    {
        public static readonly Announcer Instance = new Announcer();

        public void Announce(AnnouncementType announcementType, string message) {
            Console.WriteLine($"[{announcementType.ToString().ToUpper()}] {message}");
        }
    }
}
