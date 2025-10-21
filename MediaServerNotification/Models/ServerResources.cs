using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Models
{
    public class ServerResources
    {
        /// <summary>
        /// The CPU usage of the host
        /// </summary>
        public double HostCpuUsagePercent;

        /// <summary>
        /// The CPU usage of the server application
        /// </summary>
        public double ProcessCpuUsagePercent;

        /// <summary>
        /// The memory usage of the host
        /// </summary>
        public double HostMemoryUsagePercent;

        /// <summary>
        /// The memory usage of the server application
        /// </summary>
        public double ProcessMemoryUsagePercent;
    }
}
