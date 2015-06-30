using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using System.Globalization;

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

				Vessel vessel = FlightGlobals.ActiveVessel;
				//check if sourceVessel has a gps receiver partModule
				Part sensorPart = vessel.Parts.Find(t => t.name == "kspSensorPart");

				if (!sensorPart || !IsOperational){
					IsOperational = false;
					return 0;
				}

				//Debug.Log("found kspSensorPart");
				if(sensorPart.Modules.Contains("KSPSensors")) {
					PartModule sensorModule = sensorPart.Modules["KSPSensors"];

					//Debug.Log("Found KerbalGPS Module in ReceiverPart");
					BaseField altitudeField = sensorModule.Fields["gdAltitude"];

					//Debug.Log("Found num sats field: guiName=" + numSatField.guiName);
					//Debug.Log("checking value(host=receiverModule)=" + numSatField.GetValue(receiverModule));
					double alt = double.Parse (altitudeField.GetValue (sensorModule).ToString ());
					return alt + UnityEngine.Random.value * NoiseMargin * alt;
				} //endif module found
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
					coords [0] = (double)(vessel.latitude + UnityEngine.Random.value * NoiseMargin * vessel.latitude);
					coords [1] = (double)(vessel.longitude + UnityEngine.Random.value * NoiseMargin * vessel.longitude);
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