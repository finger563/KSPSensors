using System.Collections.Generic;
using System.IO;
using System.Threading;

using UnityEngine;
using KSP;

using KRPC.Service;
using KRPC.Service.Attributes;

using KSPAPIExtensions;
using FerramAerospaceResearch;

namespace KSPSensors
{
	[KRPCService (GameScene = GameScene.Flight)]
	public static class Sensors 
	{
		[KRPCClass]
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
		[KRPCClass]
		public class AirSpeedSensor : PartModule 
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
						return vessel.speed + Random.value * NoiseMargin * vessel.speed;
					else
						return 0;
				}
			}
		}
		[KRPCClass]
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
						coords [0] = vessel.latitude + Random.value * NoiseMargin * vessel.latitude;
						coords [1] = vessel.longitude + Random.value * NoiseMargin * vessel.longitude;
					}
					return coords;
				}
			}
		}
	}
}