using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

using UnityEngine;
using KRPC.Service;
using KRPC.Service.Attributes;

[KRPCService (GameScene=GameScene.Flight)]
public static class Sensors 
{
	[KRPCProcedure]
	public static System.Collections.Generic.IList<KSPSensor> GetSensors ()
	{
		List<KSPSensor> sensors = new List<KSPSensor> ();
		Vessel vessel = FlightGlobals.ActiveVessel;
		List<Part> partsList = vessel.Parts.FindAll (t => t.Modules.Contains ("KSPSensor"));
		foreach (Part p in partsList) {
			KSPSensor sensor = (KSPSensor)p.Modules.GetModule(0);
			sensors.Add (sensor);
		}
		return sensors;
	}
	[KRPCProcedure]
	public static System.Collections.Generic.IList<string> GetSensorTags ()
	{
		List<string> tags = new List<string> ();
		Vessel vessel = FlightGlobals.ActiveVessel;
		List<Part> partsList = vessel.Parts.FindAll (t => t.Modules.Contains ("KSPSensor"));
		foreach (Part p in partsList) {
			KSPSensor sensor = (KSPSensor)p.Modules[0];
			string sTag = sensor.sensorTag;
			tags.Add (sTag);
		}
		return tags;
	}
	[KRPCProcedure]
	public static KSPSensor GetSensor (string sTag)
	{
		Vessel vessel = FlightGlobals.ActiveVessel;
		List<Part> partsList = vessel.Parts.FindAll (t => t.Modules.Contains ("KSPSensor"));
		foreach (Part p in partsList) {
			KSPSensor s = (KSPSensor)p.Modules [0];
			if (s.sensorTag == sTag)
				return s;
		}
		throw new ArgumentException ("No such tag");
	}
}

[KRPCClass (Service = "Sensors")]
public class KSPSensor : PartModule
{
    /////////////////////////////////////////////////////////////////////////////////////////////
    //
    //    Public Variables
    //
    /////////////////////////////////////////////////////////////////////////////////////////////

    [KSPField]
    public string GNSSacronym = NULL_ACRONYM;

    [KSPField]
    public string SBASacronym = NULL_ACRONYM;

    [KSPField]
    public string EarthTime = "FALSE";

    [KSPField(isPersistant = false, guiActive = true, guiName = "Position")]
    public string gsPosition;

	[KSPField(isPersistant = false, guiActive = true, guiName = "Altitude - GNSS")]
	public string gsAltitude;

    [KSPField(isPersistant = false, guiActive = true, guiName = "Visible Satellites")]
    public UInt16 guNumSats;

    [KSPField(isPersistant = false, guiActive = true, guiName = "Accuracy")]
    public string gsAccuracy;

    public List<string> GNSSSatelliteNames = new List<string>();
    public List<Guid> GNSSSatelliteIDs = new List<Guid>();

	private TagWindow typingWindow;
	[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Sensor Tag")]
	public string sensorTag = "KSPSensor";

	[KRPCProperty]
	public string SensorTag
	{
		get { return sensorTag; }
		set { sensorTag = value; }
	}

	[KSPEvent(guiActive = true,
		guiActiveEditor = true,
		guiName = "Change Sensor Tag")]
	public void PopupTagRename()
	{
		if (typingWindow != null)
			typingWindow.Close();
		GameObject gObj = new GameObject("sensorTag", typeof(TagWindow));
		DontDestroyOnLoad(gObj);
		typingWindow = (TagWindow)gObj.GetComponent(typeof(TagWindow));
		typingWindow.Invoke(this, sensorTag);
	}

	public void TypingDone(string newValue)
	{
		sensorTag = newValue;
		TypingCancel();
	}

	public void TypingCancel()
	{
		typingWindow.Close();
		typingWindow = null;
	}

	[KSPField(isPersistant = true, guiActive = true, guiName = "Operational")]
	public bool operational = true;

	[KRPCProperty]
	public bool Operational
	{
		get { return operational; }
	}
	[KRPCMethod]
	public void Fail()
	{
		FailSensor ();
	}
	[KRPCMethod]
	public void Repair()
	{
		RepairSensor ();
	}

	[KSPEvent(guiActive = true, guiName = "Fail Sensor", name = "GNSS Receiver")]
	public void FailSensor()
	{
		Events["RepairSensor"].active = true;
		Events["FailSensor"].active = false;
		operational = false;
	}
	[KSPEvent(guiActive = true, guiName = "Repair Sensor", name = "GNSS Receiver")]
	public void RepairSensor()
	{
		Events["FailSensor"].active = true;
		Events["RepairSensor"].active = false;
		operational = true;
	}

	[KRPCProperty]
	public System.Collections.Generic.IList<double> PositionVector {
		get {
			if (!operational)
				throw new InvalidOperationException ("The sensor is no longer operational.");
			List<double> retList = new List<double> ();
			Vector3 pos = part.Rigidbody.position;
			if (guNumSats >= 4)
				pos = gfPosition;
			retList.Add (pos.x);
			retList.Add (pos.y);
			retList.Add (pos.z);
			return retList;
		}
	}
	
	[KSPField(isPersistant = false, guiActive = true, guiName = "Longitude")]
	public double longitude;
	
	[KSPField(isPersistant = false, guiActive = true, guiName = "Latitude")]
	public double latitude;

	[KSPField(isPersistant = false, guiActive = true, guiName = "Altitude")]
	public double altitude;

	[KRPCProperty]
	public System.Collections.Generic.IList<double> LatLonAlt {
		get {
			if (!operational)
				throw new InvalidOperationException ("The sensor is no longer operational.");
			List<double> retList = new List<double> ();
			retList.Add (latitude);
			retList.Add (longitude);
			retList.Add (altitude);
			return retList;
		}
	}

	[KRPCProperty]
	public double Speed {
		get {
			return 0.0;
		}
	}

    /////////////////////////////////////////////////////////////////////////////////////////////
    //
    //    Private Variables
    //
    /////////////////////////////////////////////////////////////////////////////////////////////

    private GPS_Calculations clsGPSMath = new GPS_Calculations();
    private Rect varWindowPos;

    private Vector3 gfPosition;
    private DateTime gLastSVCheckTime;
    private float gfPositionErrorEstimate = 999.9f;
    private float gfDeltaTime = 0.0f;
    private float gfFilteredAltitude = 0.0f;
    private float gfDestLat = -0.1033f;
    private float gfDestLon = -74.575f;
    private float gfOrigLat = -0.1033f;
    private float gfOrigLon = -74.575f;
    private bool gyKerbalGPSInitialised = false;
    private bool gyReceiverOn = true;
    private uint gbDisplayMode = MODE_GPS_POSITION;
    private int  giWindowID;
    private int  giLastVesselCount = 0;
    private int  giTransmitterID = FIGARO_TRANSMITTER_PART_NAME.GetHashCode();

    private System.String gsLat;
    private System.String gsLon;
    private System.String gsTime;
    private System.String gsDistance;
    private System.String gsHeading;
    private System.String gsLatDeg = "0";
    private System.String gsLatMin = "06.2";
    private System.String gsLatNS = "S";
    private System.String gsLonDeg = "74";
    private System.String gsLonMin = "34.5";
    private System.String gsLonEW = "W";
    private System.String gsModeString = "Position";
    private System.String gsButtonString = "Show Destination";

    private NumberStyles varStyle = NumberStyles.Any;
    private CultureInfo varCulture = CultureInfo.CreateSpecificCulture("en-US");


    /////////////////////////////////////////////////////////////////////////////////////////////
    //
    //    Constants
    //
    /////////////////////////////////////////////////////////////////////////////////////////////

    private const string strVersion = "1";
    private const string strKSPVersion = "0.23.5";
    private const string strSubVersion = "01";

    private const float MIN_CALCULATION_INTERVAL = 0.25f; // 4 Hz GPS
    private const float GPS_GUI_WIDTH = 150.0f;
    private const float GPS_GUI_HEIGHT = 152.0f;

    private const uint MODE_GPS_POSITION = 0;
    private const uint MODE_GPS_DESTINATION = 1;
    private const uint MODE_GPS_STATUS = 2;

    private const string FIGARO_TRANSMITTER_PART_NAME = "FigaroTransmitter";

    private const string NULL_ACRONYM = "NONE";


    /////////////////////////////////////////////////////////////////////////////////////////////
    //
    //    Implementation - Public functions
    //
    /////////////////////////////////////////////////////////////////////////////////////////////

    /********************************************************************************************
    Function Name: OnLoad
    Parameters: see function definition
    Return: void
     
    Description:  Called when PartModule is loaded.
     
    *********************************************************************************************/

    public override void OnLoad(ConfigNode node)
    {
        clsGPSMath.Reset();
        gyKerbalGPSInitialised = false;

        giWindowID = DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond;

        gLastSVCheckTime = DateTime.Now;

        base.OnLoad(node);
    }


    /********************************************************************************************
    Function Name: OnAwake
    Parameters: see function definition
    Return: void
     
    Description: Called when the part is loaded, this can be more than once.
     
    *********************************************************************************************/

    public override void OnAwake()
    {
        clsGPSMath.Reset();

        Events["DeactivateReceiver"].active = true;
        Events["ActivateReceiver"].active = false;
        gyReceiverOn = true;

		if (operational) {
			Events ["FailSensor"].active = true;
			Events ["RepairSensor"].active = false;
		} else {
			Events ["FailSensor"].active = false;
			Events ["RepairSensor"].active = true;
		}

        gyKerbalGPSInitialised = false;
        giLastVesselCount = 0;
        gyReceiverOn = true;
        gbDisplayMode = MODE_GPS_POSITION;
        gLastSVCheckTime = DateTime.Now;
        
        base.OnAwake();
    }
    
    
    /********************************************************************************************
    Function Name: OnSave
    Parameters: see function definition
    Return: void
     
    Description:  Called when PartModule is saved.
     
    *********************************************************************************************/

    public override void OnSave(ConfigNode node)
    {
        this.part.customPartData = "[GPS Dest:," + gfDestLat.ToString() + "," + gfDestLon.ToString() + "]";

        base.OnSave(node);
    }


    /********************************************************************************************
    Function Name: OnUpdate
    Parameters: void
    Return: void
     
    Description: Called on non-physics update cycle
     
    *********************************************************************************************/

    public override void OnUpdate()
    {
        gfDeltaTime += TimeWarp.deltaTime;

        if( (this.vessel.rootPart.isControllable) && (this.vessel.isActiveVessel) && (gfDeltaTime > MIN_CALCULATION_INTERVAL) )
        {
            if (gyKerbalGPSInitialised)
            {
                if (gyReceiverOn)
                {
                    // Search for new GNSS satellites every 30 seconds, and then only if the number of vessels has changed:
                    TimeSpan varCheckInterval = DateTime.Now - gLastSVCheckTime;
                    if (varCheckInterval.Seconds > 30)
                    {
                        Find_GNSS_Satellites();
                        gLastSVCheckTime = DateTime.Now;
                    }

                    if (clsGPSMath.Calculate_GPS_Position(out gfPosition, out guNumSats, out gfPositionErrorEstimate, out gfFilteredAltitude))
                    {
                        // Use the built-in GetLatitude and GetLongitude functions to compute the latitude and longitude
                        gfOrigLat = (float)vessel.mainBody.GetLatitude(gfPosition);
                        gfOrigLon = (float)vessel.mainBody.GetLongitude(gfPosition);
						gfFilteredAltitude = (float)vessel.mainBody.GetAltitude (gfPosition);

                        gsLat = clsGPSMath.Lat_to_String(gfOrigLat);
                        gsLon = clsGPSMath.Lon_to_String(gfOrigLon);

                        gsPosition = gsLat + " " + gsLon;
                        gsAltitude = Math.Round(gfFilteredAltitude, 1).ToString("#0.0") + " m";
                        gsAccuracy = Math.Round(gfPositionErrorEstimate, 1).ToString("#0.0") + " m";

                        if (gbDisplayMode == MODE_GPS_POSITION)
                        {
                            gsTime = clsGPSMath.Time_to_String(Planetarium.GetUniversalTime(), (EarthTime == "TRUE"));
                        }
                        else if (gbDisplayMode == MODE_GPS_DESTINATION)
                        {
                            gsDistance = clsGPSMath.Great_Circle_Distance(gfOrigLat, gfOrigLon, gfDestLat, gfDestLon, gfFilteredAltitude);
                            gsHeading = clsGPSMath.Great_Circle_Heading(gfOrigLat, gfOrigLon, gfDestLat, gfDestLon);
                        }
                    }
                    else
                    {
                        gsTime = clsGPSMath.Time_to_String(Planetarium.GetUniversalTime(), (EarthTime == "TRUE"));
                        gsLat = "N/A";
                        gsLon = "N/A";
                        gsAltitude = "N/A";
                        gsAccuracy = "N/A";
                        gsHeading = "N/A";
                        gsDistance = "N/A";
                        gsPosition = gsLat;
                        gsAccuracy = "N/A";
                    }

                    gfDeltaTime = 0.0f;
                }
            }
            else
            {
                Initialise_KerbalGPS();
            }
		}
		Vector3 pos = part.Rigidbody.position;
		if (guNumSats >= 4)
			pos = gfPosition;
		altitude = (double)vessel.mainBody.GetAltitude (pos);
		latitude = (double)vessel.mainBody.GetLatitude (pos);
		longitude = (double)vessel.mainBody.GetLongitude (pos);
		if (longitude > 180.0f)
			longitude = longitude - 360.0f;

        base.OnUpdate();
    }


    /********************************************************************************************
    Function Name: Find_GNSS_Satellites
    Parameters: void
    Return: void
     
    Description:  Checks if the number of vessels has changed and if so, finds GNSS satellites 
    among the list of existing vessels. 
     
    *********************************************************************************************/

    public void Find_GNSS_Satellites()
    {
        if (GNSSacronym != NULL_ACRONYM) return;

        if (this.vessel == null) return;

        if (this.vessel.isActiveVessel)
        {
            if (FlightGlobals.Vessels.Count != giLastVesselCount)
            {
                GNSSSatelliteIDs.Clear();
                GNSSSatelliteNames.Clear();
                giLastVesselCount = FlightGlobals.Vessels.Count;

                foreach (Vessel varVessel in FlightGlobals.Vessels)
                {
                    // proceed if vessel being checked has a command pod, is orbiting the same celestial object and is not the active vessel
                    if ((varVessel.isCommandable) && (vessel.mainBody == varVessel.mainBody) && (varVessel != vessel))
                    {
                        foreach (ProtoPartSnapshot varPart in varVessel.protoVessel.protoPartSnapshots)
                        {
                            if (varPart.partName.GetHashCode() == giTransmitterID)
                            {
                                GNSSSatelliteNames.Add(varVessel.name);
                                GNSSSatelliteIDs.Add(varVessel.id);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////////////
    //
    //    Implementation - Private functions
    //
    /////////////////////////////////////////////////////////////////////////////////////////////

    /********************************************************************************************
    Function Name: WindowGUI
    Parameters: see function definition
    Return: see function definition
     
    Description:  Callback function to draw GUI
     
    *********************************************************************************************/

    private void WindowGUI(int windowID)
    {

        GUIStyle varButtonStyle = new GUIStyle(GUI.skin.button);
        varButtonStyle.fixedWidth = GPS_GUI_WIDTH - 5.0f;
        varButtonStyle.fixedHeight = 20.0f;
        varButtonStyle.contentOffset = new Vector2(0, 2);
        varButtonStyle.normal.textColor = varButtonStyle.focused.textColor = Color.white;
        varButtonStyle.hover.textColor = varButtonStyle.active.textColor = Color.yellow;

        GUILayout.BeginVertical(GUILayout.MaxHeight(GPS_GUI_HEIGHT));

        if (GUILayout.Button(gsButtonString, varButtonStyle))
        {
            gbDisplayMode++;
            gbDisplayMode %= 3;

            if (gbDisplayMode == MODE_GPS_POSITION)
            {
                gsModeString = "Position";
                gsButtonString = "Show Destination";
            }
            else if (gbDisplayMode == MODE_GPS_DESTINATION)
            {
                gsModeString = "Destination";
                gsButtonString = "Show Status";
            }
            else
            {
                gsModeString = "Status";
                gsButtonString = "Show Position";
            }
        }

        if (gbDisplayMode == MODE_GPS_POSITION)
        {

            GUILayout.Label("UT: " + gsTime, GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
            GUILayout.Label("Latitude: " + gsLat, GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
            GUILayout.Label("Longitude: " + gsLon, GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
            GUILayout.Label("Altitude: " + gsAltitude, GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
        }
        else if (gbDisplayMode == MODE_GPS_DESTINATION)
        {
            drawDestiationGUI(varButtonStyle);
        }
        else
        {
            GUILayout.Label("Accuracy: " + gsAccuracy);
            GUILayout.Label("Visible Sats: " + guNumSats.ToString());
        }

        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));

    }


    /********************************************************************************************
    Function Name: drawDestiationGUI
    Parameters: void
    Return: void
     
    Description:  Draw GPS GUI's Destination  window
     
    *********************************************************************************************/

    private void drawDestiationGUI(GUIStyle varButtonStyle)
    {
        GUIStyle varTextStyle = new GUIStyle(GUI.skin.textField);
        GUIStyle varHemisphereStyle = new GUIStyle(GUI.skin.textField);
        GUIStyle varLabelStyle = new GUIStyle(GUI.skin.label);

        varTextStyle.alignment = TextAnchor.UpperCenter;
        varTextStyle.normal.textColor = varTextStyle.focused.textColor = Color.white;
        varTextStyle.hover.textColor = varTextStyle.active.textColor = Color.yellow;
        varTextStyle.padding = new RectOffset(0, 0, 0, 0);
        varTextStyle.fixedHeight = 16.0f;
        varTextStyle.fixedWidth = 35.0f;

        varHemisphereStyle.alignment = TextAnchor.UpperCenter;
        varHemisphereStyle.normal.textColor = varHemisphereStyle.focused.textColor = Color.white;
        varHemisphereStyle.hover.textColor = varHemisphereStyle.active.textColor = Color.yellow;
        varHemisphereStyle.padding = new RectOffset(0, 0, 0, 0);
        varHemisphereStyle.fixedHeight = 16.0f;
        varHemisphereStyle.fixedWidth = 20.0f;

        varLabelStyle.padding = new RectOffset(0, 0, 0, 7);

        GUILayout.Label("Distance: " + gsDistance);
        GUILayout.Label("Heading: " + gsHeading);

        GUILayout.BeginVertical(GUILayout.MaxHeight(20.0f));
          GUILayout.BeginHorizontal(GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
            GUILayout.Label("Lat: ", varLabelStyle);
            gsLatDeg = GUILayout.TextArea(gsLatDeg, 3, varTextStyle);
            GUILayout.Label("°", varLabelStyle);
            gsLatMin = GUILayout.TextArea(gsLatMin, 4, varTextStyle);
            //GUILayout.Label("'", varLabelStyle);
            gsLatNS = GUILayout.TextArea(gsLatNS, 1, varHemisphereStyle);
          GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUILayout.MaxHeight(20.0f));
          GUILayout.BeginHorizontal(GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
            GUILayout.Label("Lon: ", varLabelStyle);
            gsLonDeg = GUILayout.TextArea(gsLonDeg, 3, varTextStyle);
            GUILayout.Label("°", varLabelStyle);
            gsLonMin = GUILayout.TextArea(gsLonMin, 4, varTextStyle);
            //GUILayout.Label("'", varLabelStyle);
            gsLonEW = GUILayout.TextArea(gsLonEW, 1, varHemisphereStyle);
          GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if ((gsLatDeg.Length != 0) && (gsLatMin.Length >2) && (gsLatNS.Length != 0) && (gsLonDeg.Length != 0) && (gsLonMin.Length >2) && (gsLonEW.Length != 0))
        {
            gfDestLat = ParseNS() * (ParseNumericString(gsLatDeg) + ParseNumericString(gsLatMin) / 60.0f);
            gfDestLon = ParseEW() * (ParseNumericString(gsLonDeg) + ParseNumericString(gsLonMin) / 60.0f);

            if (gfDestLat > 90) gfDestLat = 90.0f;
            if (gfDestLat < -90) gfDestLat = -90.0f;
            if (gfDestLon > 180) gfDestLon = 180.0f;
            if (gfDestLon < -180) gfDestLon = -180.0f;

            gsLatDeg = Math.Floor(Math.Abs(gfDestLat)).ToString();
            gsLatMin = ((Math.Abs(gfDestLat) - Math.Floor(Math.Abs(gfDestLat))) * 60.0f).ToString("#0.0");

            gsLonDeg = Math.Floor(Math.Abs(gfDestLon)).ToString();
            gsLonMin = ((Math.Abs(gfDestLon) - Math.Floor(Math.Abs(gfDestLon))) * 60.0f).ToString("#0.0");
        }
        else
        {
            if ((gsLatDeg.Length == 0) || (gsLatNS.Length == 0) || (gsLonDeg.Length == 0) || (gsLonEW.Length == 0))
            {
                if (gsLatMin.StartsWith(".")) gsLatMin = "0" + gsLatMin;
                if (gsLonMin.StartsWith(".")) gsLonMin = "0" + gsLonMin;
                if (gsLatMin.Length <= 2) gsLatMin = gsLatMin + ".0";
                if (gsLonMin.Length <= 2) gsLonMin = gsLonMin + ".0";
            }
        }

        if (GUILayout.Button("Here", varButtonStyle))
        {
            gsLatDeg = Math.Floor(Math.Abs(gfOrigLat)).ToString();
            gsLatMin = ((Math.Abs(gfOrigLat) - Math.Floor(Math.Abs(gfOrigLat))) * 60.0f).ToString("#0.0");

            if (gfOrigLon > 180.0f)  gfOrigLon -= 360.0f;
            if (gfOrigLon < -180.0f) gfOrigLon += 360.0f;
            
            gsLonDeg = Math.Floor(Math.Abs(gfOrigLon)).ToString();
            gsLonMin = ((Math.Abs(gfOrigLon) - Math.Floor(Math.Abs(gfOrigLon))) * 60.0f).ToString("#0.0");
        }

    }


    /********************************************************************************************
    Function Name: drawGUI
    Parameters: see function definition
    Return: see function definition
     
    Description:  Initiate an instance of the GUI and assign a callback funcction to draw it.
     
    *********************************************************************************************/

    private void drawGUI()
    {
		return;
        try
        {
            if ((this.part.State != PartStates.DEAD) && (this.vessel.isActiveVessel))
            {
                GUI.skin = HighLogic.Skin;
                varWindowPos = GUILayout.Window(giWindowID, varWindowPos, WindowGUI, "Figaro - " + gsModeString, GUILayout.MinWidth(GPS_GUI_WIDTH), GUILayout.MaxHeight(GPS_GUI_HEIGHT) );
            }
            else
            {
                RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI if part has been deleted
            }
        }
        catch
        {
            RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI if part has been deleted
        }
    }


    /********************************************************************************************
    Function Name: ActivateReceiver
    Parameters: see function definition
    Return: see function definition
     
    Description:  Toggle GNSS receiver on
     
    *********************************************************************************************/

    [KSPEvent(guiActive = true, guiName = "Turn on receiver", name = "Figaro GNSS Receiver")]
    public void ActivateReceiver()
    {
        Find_GNSS_Satellites();
        Events["DeactivateReceiver"].active = true;
        Events["ActivateReceiver"].active = false;
        gyReceiverOn = true;

        // Open the receiver UI
        RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI)); //start the GUI
    }


    /********************************************************************************************
    Function Name: DeactivateReceiver
    Parameters: see function definition
    Return: see function definition
     
    Description:  Toggle GNSS receiver off
     
    *********************************************************************************************/

    [KSPEvent(guiActive = true, guiName = "Turn off receiver", name = "Figaro GNSS Receiver")]
    public void DeactivateReceiver()
    {
        Events["DeactivateReceiver"].active = false;
        Events["ActivateReceiver"].active = true;
        gyReceiverOn = false;

        // close the receiver UI and remove callback function
        RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI));
    }


    /********************************************************************************************
    Function Name: Initialise_KerbalGPS
    Parameters: see function definition
    Return: see function definition
     
    Description:  Initialises GPS part module and GUI
     
    *********************************************************************************************/

    private void Initialise_KerbalGPS()
    {
        if (!gyKerbalGPSInitialised)
        {
            print("[Kerbal GPS Module] Loaded Version " + strVersion + "." + strKSPVersion + "." + strSubVersion);
            print("[Kerbal GPS Module] Reference GNSS Acronym: " + GNSSacronym); 

            gyKerbalGPSInitialised = true;
            giLastVesselCount = 0;

            Find_GNSS_Satellites();

            clsGPSMath.Initialise(this, GNSSacronym, SBASacronym);

            if ((varWindowPos.x == 0) && (varWindowPos.y == 0))
            {
                varWindowPos = new Rect(Screen.width / 5, (7 * Screen.height) / 10, GPS_GUI_WIDTH, GPS_GUI_HEIGHT);
            }

            ActivateReceiver();

			if (operational) {
				Events ["FailSensor"].active = true;
				Events ["RepairSensor"].active = false;
			} else {
				Events ["FailSensor"].active = false;
				Events ["RepairSensor"].active = true;
			}
        }
    }


    /********************************************************************************************
    Function Name: ParseNumericString
    Parameters: see function definition
    Return: see function definition
     
    Description:  Parses a numeric string into a floating point number. Returns 0 on failure.
     
    *********************************************************************************************/

    private float ParseNumericString(string strNumber)
    {
        float fReturn;

        if (!float.TryParse(strNumber, varStyle, varCulture, out fReturn)) fReturn = 0.0f;

        return fReturn;
    }


    /********************************************************************************************
    Function Name: ParseNS
    Parameters: see function definition
    Return: see function definition
     
    Description:  Parses a NS string into a floating point number. Returns 0 on failure.
     
    *********************************************************************************************/

    private float ParseNS()
    {
        float fReturn;

        if (gsLatNS == "N" || gsLatNS == "n" )
        {
            fReturn = 1.0f;
        }
        else if (gsLatNS == "S" || gsLatNS == "s")
        {
            fReturn = -1.0f;
        }
        else
        {
            fReturn = 0.0f;
            gsLatNS = "N";
        }

        return fReturn;
    }


    /********************************************************************************************
    Function Name: ParseEW
    Parameters: see function definition
    Return: see function definition
     
    Description:  Parses EW string into a floating point number. Returns 0 on failure.
     
    *********************************************************************************************/

    private float ParseEW()
    {
        float fReturn = 0.0f;

        if (gsLonEW == "E" || gsLonEW == "e")
        {
            fReturn = 1.0f;
        }
        else if (gsLonEW == "W" || gsLonEW == "w")
        {
            fReturn = -1.0f;
        }
        else
        {
            fReturn = 0.0f;
            gsLonEW = "E";
        }

        return fReturn;
    }

}

//
// END OF FILE
//

