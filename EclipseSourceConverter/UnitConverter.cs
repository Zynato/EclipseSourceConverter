using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    static class UnitConverter
    {
        // From: http://stackoverflow.com/a/4044406

        /// <summary>
        /// Converts an integer value in twips to the corresponding integer value
        /// in pixels on the x-axis.
        /// </summary>
        /// <param name="source">The Graphics context to use</param>
        /// <param name="inTwips">The number of twips to be converted</param>
        /// <returns>The number of pixels in that many twips</returns>
        public static int ConvertTwipsToXPixels(int twips) {
            // TODO: Make 267 configurable (PPI)
            return (int)(((double)twips) * (1.0 / 1440.0) * 267);
        }

        /// <summary>
        /// Converts an integer value in twips to the corresponding integer value
        /// in pixels on the y-axis.
        /// </summary>
        /// <param name="source">The Graphics context to use</param>
        /// <param name="inTwips">The number of twips to be converted</param>
        /// <returns>The number of pixels in that many twips</returns>
        public static int ConvertTwipsToYPixels(int twips) {
            // TODO: Make 267 configurable (PPI)
            return (int)(((double)twips) * (1.0 / 1440.0) * 267);
        }
    }
}
