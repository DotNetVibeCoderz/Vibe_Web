using System;

namespace AISTracker.Models
{
    public class VesselTrackingInfo
    {
        public string MMSI { get; set; }
        public string Name { get; set; }
        public string IMONumber { get; set; }
        public string Flag { get; set; }
        public string VesselType { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public string VesselStatus { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }
        public DateTime LastUpdate { get; set; }
        public string NavigationalStatus { get; set; }
    }
}
