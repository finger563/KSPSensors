using System.Collections.Generic;
using System.IO;
using System.Threading;

using UnityEngine;
using KSP;

using KRPC.Service;
using KRPC.Service.Attributes;

namespace KSPSensors
{
	[KRPCClass (Service = "Sensors")]
	public class AltitudeSensor : PartModule 
	{
		[KRPCProperty]
		public bool IsOperational {
			get;
			set;
		}

		[KRPCProperty]
		public double NoiseMargin {
			get;
			set;
		}

		[KRPCProperty]
		public double Value {
			get {
				if (IsOperational)
					return vessel.altitude + Random.value * NoiseMargin * vessel.altitude;
				else
					return 0;
			}
		}
	}
	[KRPCClass (Service = "Sensors")]
	public class GPSSensor : PartModule 
	{
		[KRPCProperty]
		public bool IsOperational {
			get;
			set;
		}

		[KRPCProperty]
		public double NoiseMargin {
			get;
			set;
		}

		[KRPCProperty]
		public System.Collections.Generic.IList<double> Value {
			get {
				System.Collections.Generic.IList<double> coords = new List<double>();
				coords.Add (0);
				coords.Add (0);
				if (IsOperational) {
					coords [0] = (double)(vessel.latitude + Random.value * NoiseMargin * vessel.latitude);
					coords [1] = (double)(vessel.longitude + Random.value * NoiseMargin * vessel.longitude);
				}
				return coords;
			}
		}
	}
	[KRPCService (GameScene = GameScene.Flight)]
	public static class Sensors 
	{
		public static System.Collections.Generic.IList<AltitudeSensor> altSensors = new List<AltitudeSensor>();
		public static System.Collections.Generic.IList<GPSSensor> gpsSensors = new List<GPSSensor>();
		[KRPCProcedure]
		public static AltitudeSensor CreateAltitudeSensor(double noise)
		{
			AltitudeSensor sensor = new AltitudeSensor ();
			sensor.NoiseMargin = noise;
			sensor.IsOperational = true;
			altSensors.Add (sensor);
			return sensor;
		}
		[KRPCProcedure]
		public static GPSSensor CreateGPSSensor(double noise)
		{
			GPSSensor sensor = new GPSSensor ();
			sensor.NoiseMargin = noise;
			sensor.IsOperational = true;
			gpsSensors.Add (sensor);
			return sensor;
		}
	}
}