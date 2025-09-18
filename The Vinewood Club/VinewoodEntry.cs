using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using System.Net;
using System.IO;

public class VinewoodEntry : Script
{
    public static Vector3 Entry = new Vector3(1218.465f, -3234.697f, 4.52875f);
    public static Vector3 Exit = new Vector3(1212.767f, -3252.277f, -49.99775f);
    public static Vector3 Garage = new Vector3(1210.181f, -3252.594f, -49.99775f);
    public static Vector3 Street = new Vector3(1218.144f, -3226.999f, 4.88975f);
    public static float GaragePlayerAngle = 100.7243f;
    public static float StreetPlayerAngle = 3.314108f;
    public static int GarageInteriorID = 0;
    public static Blip VinewoodCarClub;
    private const string VehicleListUrl = "https://raw.githubusercontent.com/sruckstar/VinewoodCarClub/main/VinewoodCarClub/VehicleList.txt";
    private static ScriptSettings config_settings;

    public static List<string> models_peds = new List<string>() {
    "A_F_Y_CarClub_01",
    "A_M_Y_CarClub_01",
    };

    private class PedConfig
    {
        public string Name;
        public string AnimDict;
        public string AnimName;
        public Vector3 Position;
        public float Heading;
        public bool CanSpawn { get; set; } = true;
    }

    public class VehicleConfig
    {
        public string Name { get; }
        public int ColorPrimary { get; }
        public int ColorSecondary { get; }
        public int Livery { get; }
        public bool CanSpawn { get; set; } = true;

        public VehicleConfig(string name, int colorPrimary, int colorSecondary, int livery = -1)
        {
            Name = name;
            ColorPrimary = colorPrimary;
            ColorSecondary = colorSecondary;
            Livery = livery;
            CanSpawn = true;
        }
    }

    public static List<VehicleConfig> Vehicles = new List<VehicleConfig>();

    private readonly PedConfig[] pedConfigs = new PedConfig[]
    {
        new PedConfig { Name = "A_F_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@female_b@base", AnimName = "base",   Position = new Vector3(1206.819f, -3248.682f, -50f), Heading = 43.86293f, CanSpawn = true }, //0
        new PedConfig { Name = "A_M_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@male_a@base",   AnimName = "base",   Position = new Vector3(1201.903f, -3251.494f, -50f), Heading = 349.7142f, CanSpawn = true }, //2
        new PedConfig { Name = "A_M_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@male_b@idles",  AnimName = "idle_c", Position = new Vector3(1200.211f, -3250.837f, -50f), Heading = 328.1486f, CanSpawn = true }, //2
        new PedConfig { Name = "A_M_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@male_a@idles",  AnimName = "idle_d", Position = new Vector3(1189.059f, -3255.431f, -50f), Heading = 60.43689f, CanSpawn = true }, //9
        new PedConfig { Name = "A_F_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@male_c@base",   AnimName = "base",   Position = new Vector3(1188.694f, -3257.009f, -50f), Heading = 59.67503f, CanSpawn = true }, //9
        new PedConfig { Name = "A_F_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@male_a@base",   AnimName = "base",   Position = new Vector3(1195.416f, -3253.988f, -50f), Heading = 191.1719f, CanSpawn = true }, //7
        new PedConfig { Name = "A_M_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@male_b@idles",  AnimName = "idle_c", Position = new Vector3(1200.378f, -3254.95f,  -50f), Heading = 161.8084f, CanSpawn = true }, //6
        new PedConfig { Name = "A_M_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@female_b@base", AnimName = "base",   Position = new Vector3(1206.134f, -3256.563f, -50f), Heading = 135.4807f, CanSpawn = true }, //5
        new PedConfig { Name = "A_M_Y_CarClub_01", AnimDict = "anim@amb@carmeet@checkout_car@male_d@base",   AnimName = "base",   Position = new Vector3(1203.26f,  -3255.629f, -50f), Heading = 226.9537f, CanSpawn = true } //5
    };

    public static List<(Vector3 Position, float RotationZ)> Transforms = new List<(Vector3, float)>
    {
        ( new Vector3(1205.019f, -3247.305f,  -49.29789f),  -179.0011f),
        ( new Vector3(1210.323f, -3247.167f,  -49.29790f),   179.9950f),
        ( new Vector3(1200.086f, -3247.259f,  -49.29790f),  -174.9892f),
        ( new Vector3(1190.557f, -3247.625f,  -49.29789f),  -179.9800f),
        ( new Vector3(1195.321f, -3247.751f,  -49.29788f),   179.9937f),
        ( new Vector3(1204.268f, -3258.586f,  -49.29788f),   -4.95759f),
        ( new Vector3(1200.252f, -3258.054f,  -49.29788f),    4.543178f),
        ( new Vector3(1196.487f, -3258.114f,  -49.29789f),   -5.734032f),
        ( new Vector3(1192.247f, -3257.677f,  -49.29789f),   -1.508073f),
        ( new Vector3(1182.570f, -3252.597f,  -49.29789f),   -34.98712f)
    };

    private readonly List<Ped> createdPeds = new List<Ped>();
    private readonly List<Vehicle> createdVehicles = new List<Vehicle>();

    public VinewoodEntry()
    {
        Tick += OnTick;
        Aborted += OnAborted;

        config_settings = ScriptSettings.Load($"Scripts\\VinewoodCarClub\\settings.ini");
        int veh_load = config_settings.GetValue<int>("MAIN", "ONLINE_SYNCHRONIZATION", 1);

        LoadGarage();
        LoadVehicleList(veh_load);
        GarageInteriorID = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, 1200.00f, -3250.00f, -50.00f);
        VinewoodCarClub = World.CreateBlip(Entry);
        VinewoodCarClub.Sprite = BlipSprite.TheVinewoodCarClub;
        VinewoodCarClub.IsShortRange = true;
    }

    private void LoadVehicleList(int mode)
    {
        string vehicleListData = null;

        try
        {
            if (mode == 1)
            {
                WebClient client = new WebClient();
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                vehicleListData = client.DownloadString(VehicleListUrl);
            }
            else
            {
                vehicleListData = File.ReadAllText("Scripts\\VinewoodCarClub\\VehicleList.txt");
            }
           
            StringReader reader = new StringReader(vehicleListData);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

                string[] parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    string modelName = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int colorPrimary) &&
                        int.TryParse(parts[2].Trim(), out int colorSecondary))
                    {
                        int livery = -1;
                        if (parts.Length > 3 && int.TryParse(parts[3].Trim(), out int parsedLivery))
                        {
                            livery = parsedLivery;
                        }
                        Vehicles.Add(new VehicleConfig(modelName, colorPrimary, colorSecondary, livery));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GTA.UI.Notification.PostTicker($"Failed to load vehicles: {ex.Message}", true);
        }
    }

    private void OnAborted(object sender, EventArgs e)
    {
        if (VinewoodCarClub != null && VinewoodCarClub.Exists())
        {
            VinewoodCarClub.Delete();
            DeletePeds();
            DeleteVehicles();
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        World.DrawMarker(MarkerType.VerticalCylinder, Entry, Vector3.Zero, Vector3.Zero, new Vector3(1.0f, 1.0f, 1.0f), Color.LightBlue);
        World.DrawMarker(MarkerType.VerticalCylinder, Exit, Vector3.Zero, Vector3.Zero, new Vector3(1.0f, 1.0f, 1.0f), Color.LightBlue);

        if (Game.Player.Character.Position.DistanceTo(Entry) < 1.5f)
        {
            GTA.UI.Screen.ShowHelpTextThisFrame("Press ~INPUT_CONTEXT~ to enter garage");
            if (Game.IsControlJustPressed(GTA.Control.Context))
            {
                GTA.UI.Screen.FadeOut(500);
                Wait(500);

                SetupPeds();
                SetupVehicles();

                Game.Player.Character.Position = Garage;
                Game.Player.Character.Heading = GaragePlayerAngle;

                Wait(500);
                GTA.UI.Screen.FadeIn(500);
            }
        }
        else if (Game.Player.Character.Position.DistanceTo(Exit) < 1.5f)
        {
            GTA.UI.Screen.ShowHelpTextThisFrame("Press ~INPUT_CONTEXT~ to exit garage");
            if (Game.IsControlJustPressed(GTA.Control.Context))
            {
                GTA.UI.Screen.FadeOut(500);
                Wait(500);

                Game.Player.Character.Position = Street;
                Game.Player.Character.Heading = StreetPlayerAngle;

                DeletePeds();
                DeleteVehicles();

                GTA.UI.Screen.FadeIn(500);
            }
        }

        if (createdVehicles.Count > 0)
        {
            int index = 0;
            foreach (Vehicle vehicle in createdVehicles.ToArray())
            {
                if (Game.Player.Character.CurrentVehicle == vehicle)
                {

                    if (Game.IsControlJustPressed(GTA.Control.VehicleAccelerate) ||
                        Game.IsControlJustPressed(GTA.Control.VehicleBrake))
                    {
                        if (Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, Game.Player.Character.Position.X,
                                Game.Player.Character.Position.Y,
                                Game.Player.Character.Position.Z) == GarageInteriorID)
                        {

                            GTA.UI.Screen.FadeOut(500);
                            Wait(500);

                            Vehicles[index].CanSpawn = false;
                            vehicle.Position = Street;
                            vehicle.Heading = StreetPlayerAngle;
                            vehicle.IsPersistent = false;
                            createdVehicles.Remove(vehicle);

                            DeletePeds();
                            DeleteVehicles();

                            switch (index)
                            {
                                case 0:
                                    pedConfigs[0].CanSpawn = false;
                                    break;
                                case 2:
                                    pedConfigs[1].CanSpawn = false;
                                    pedConfigs[2].CanSpawn = false;
                                    break;
                                case 5:
                                    pedConfigs[7].CanSpawn = false;
                                    pedConfigs[8].CanSpawn = false;
                                    break;
                                case 6:
                                    pedConfigs[6].CanSpawn = false;
                                    break;
                                case 7:
                                    pedConfigs[5].CanSpawn = false;
                                    break;
                                case 9:
                                    pedConfigs[3].CanSpawn = false;
                                    pedConfigs[4].CanSpawn = false;
                                    break;
                            }

                            Wait(500);
                            GTA.UI.Screen.FadeIn(500);
                        }
                    }
                }

                index++;
            }
        }
    }

    private void LoadGarage()
    {
        Function.Call(Hash.ON_ENTER_MP);
        Function.Call(Hash.SET_INSTANCE_PRIORITY_MODE, 1);
        Function.Call(Hash.REQUEST_IPL, "m23_1_garage");
        int GarageID = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, 1200.00f, -3250.00f, -50.00f);
        Function.Call(Hash.ACTIVATE_INTERIOR_ENTITY_SET, GarageID, "entity_set_backdrop_frames");
        Function.Call(Hash.ACTIVATE_INTERIOR_ENTITY_SET, GarageID, "entity_set_plus");
        Function.Call(Hash.ACTIVATE_INTERIOR_ENTITY_SET, GarageID, "entity_set_signs");
        Function.Call(Hash.REFRESH_INTERIOR, GarageID);
    }

    private void SetupPeds()
    {
        foreach (PedConfig cfg in pedConfigs)
        {
            if (!cfg.CanSpawn) continue;

            Ped ped = World.CreatePed(cfg.Name, cfg.Position, cfg.Heading);
            createdPeds.Add(ped);
            ped.Task.PlayAnimation(cfg.AnimDict, cfg.AnimName, 8.0f, -1, AnimationFlags.Loop);
            Function.Call(GTA.Native.Hash.FREEZE_ENTITY_POSITION, ped, true);
        }
    }

    private void SetupVehicles()
    {
        foreach (var cfg in Vehicles)
        {
            if (!cfg.CanSpawn) continue;

            var model = new Model(cfg.Name);
            model.Request(1000);
            if (!model.IsLoaded) continue;

            int index = Vehicles.IndexOf(cfg);
            if (index >= Transforms.Count) continue;

            Vector3 position = Transforms[index].Position;
            float rotationZ = Transforms[index].RotationZ;

            var veh = World.CreateVehicle(model, position, rotationZ);
            if (veh == null) continue;

            veh.Mods.InstallModKit();
            veh.Mods.PrimaryColor = (VehicleColor)cfg.ColorPrimary;
            veh.Mods.SecondaryColor = (VehicleColor)cfg.ColorSecondary;
            if (cfg.Livery >= 0 && cfg.Livery < veh.Mods.LiveryCount)
                veh.Mods.Livery = cfg.Livery;

            veh.IsPersistent = true;
            createdVehicles.Add(veh);
            model.MarkAsNoLongerNeeded();
        }
    }

    private void DeletePeds()
    {
        foreach (Ped ped in createdPeds)
        {
            if (ped != null && ped.Exists())
            {
                ped.Delete();
            }
        }
    }

    private void DeleteVehicles()
    {
        foreach (Vehicle veh in createdVehicles)
        {
            if (veh != null && veh.Exists())
            {
                veh.Delete();
            }
        }
    }
}