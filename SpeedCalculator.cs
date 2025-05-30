using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownSarSoftApp.DownloadCore
{
    public class SpeedCalculator
    {
        private DateTime _lastUpdateTime;
        private long _lastBytes;

        public SpeedCalculator()
        {
            _lastUpdateTime = DateTime.Now;
            _lastBytes = 0;
        }

        public double CalculateSpeed(long currentBytes)
        {
            var now = DateTime.Now;
            var timeDiff = (now - _lastUpdateTime).TotalSeconds;
            if (timeDiff <= 0) return 0;

            var bytesDiff = currentBytes - _lastBytes;
            double speed = bytesDiff / timeDiff; // bytes per second

            _lastBytes = currentBytes;
            _lastUpdateTime = now;

            return speed;
        }

        public TimeSpan CalculateTimeLeft(long totalBytes, long downloadedBytes, double speedBytesPerSecond)
        {
            if (speedBytesPerSecond <= 0)
                return TimeSpan.MaxValue;

            long remaining = totalBytes - downloadedBytes;
            double secondsLeft = remaining / speedBytesPerSecond;

            return TimeSpan.FromSeconds(secondsLeft);
        }
    }
}