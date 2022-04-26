using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine;
using System.Reflection;
using System.Linq;
using PolyTechFramework;

namespace AngryBirds {

    public enum ControlState{
        MOVING,
        PULLING,
        WAITING,
        ANIMATING,
        MOVING_SPECIAL,
        NON_SIM
    }
    public static class GlobalManager{
        public static Vehicle[] vehicles;
        public static Vector3 slingshot_position;
        public static Vehicle current_vehicle;

        public static ControlState controlState;
        public static void enter_sim(){
            current_vehicle = vehicles[0];
        }

        public static void sim_tick(){

        }

        public static void exit_sim(){

        }

    }
    public static class PullManager{
        public static FieldInfo allBodies = typeof(PolyPhysics.Vehicle).GetField("allBodies", BindingFlags.NonPublic | BindingFlags.Instance);
        public static FieldInfo m_Physics = typeof(Vehicle).GetField("m_Physics", BindingFlags.NonPublic | BindingFlags.Instance);
        public static FieldInfo motion = typeof(PolyPhysics.Rigidbody).GetField("motion", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float max_pull = 2f;
        public const float mouse_transform_scale = .005f;
        public const float sling_distance_scaler = 10f;
        public static GameObject stringObject;
        public static LineRenderer stringRenderer;
        public static Vector3 mouseStart;
        public static Vector3 pullStart;
        public static Vector3 offset(Vehicle vehicle){
            // TODO: implement a switch for different types of vehicles
            return new Vector3(-1f, 0f, 0f);
        }
        public static void enter(){
            // vv This Code is Temporary and should be removed
            GlobalManager.slingshot_position = (Vector3)GlobalManager.current_vehicle.transform.position;
            // ^^
            mouseStart = Input.mousePosition;
            pullStart = GlobalManager.slingshot_position;
            stringObject = new GameObject();
            stringObject.transform.position = pullStart;
            stringObject.AddComponent<LineRenderer>();
            stringRenderer = stringObject.GetComponent<LineRenderer>();
            stringRenderer.material = new Material(Shader.Find("Sprites/Default"));
            stringRenderer.startColor = new Color(1f,1f,1f,1f);
            stringRenderer.endColor = new Color(1f,1f,1f,1f);
            stringRenderer.startWidth = .1f;
            stringRenderer.endWidth = .1f;
            stringRenderer.positionCount = 2;
            stringRenderer.SetPosition(0, pullStart);
            stringRenderer.SetPosition(1, pullStart + offset(GlobalManager.current_vehicle));
        }

        public static void tick(){
            Vector3 pull_difference = mouse_transform_scale * (Input.mousePosition - mouseStart);
            if(pull_difference.magnitude > max_pull){
                pull_difference = Vector3.ClampMagnitude(pull_difference, max_pull);
            }
            stringRenderer.SetPosition(1, pullStart + pull_difference + offset(GlobalManager.current_vehicle));
        }

        public static void exit(){
            GameObject.Destroy(stringObject);
            Vector3 pull_difference = mouse_transform_scale * (Input.mousePosition - mouseStart);
            if(pull_difference.magnitude > max_pull){
                pull_difference = Vector3.ClampMagnitude(pull_difference, max_pull);
            }
            Vector3 restoring3 = -1 * pull_difference * mouse_transform_scale * sling_distance_scaler;
            Vector2 restoring2 = new Vector2(restoring3.x, restoring3.y);
            PolyPhysics.Rigidbody[] bodies = (PolyPhysics.Rigidbody[])allBodies.GetValue((PolyPhysics.Vehicle)m_Physics.GetValue(GlobalManager.current_vehicle));
            foreach(PolyPhysics.Rigidbody body in bodies){
                Poly.Physics.Solver.Motion vehicleMotion = ((Poly.Physics.Solver.Motion)motion.GetValue(body));
                vehicleMotion.linVel = restoring2;
                motion.SetValue(body,vehicleMotion);
            }
            GlobalManager.controlState = ControlState.MOVING;
        }
        
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    
    public class AngryBirds : PolyTechMod {

        public static GameObject slingshotString;
        public static LineRenderer slingshotLine;

        public static Vector3 pullStart;
        public static Vector3 carStart;
        public static FieldInfo _t2 = typeof(PolyPhysics.Rigidbody).GetField("_t2", BindingFlags.NonPublic | BindingFlags.Instance);

        //public static ControlState controlState = ControlState.NON_SIM;
        public const string pluginGuid = "polytech.AngryBirds";
        public const string pluginName = "Angry Birds";
        public const string pluginVersion = "1.0.0";

        public static GameObject[] VehicleObjects = new GameObject[] {};
        public static Vehicle[] Vehicles = new Vehicle[] {};

        public static GameObject[] VehicleCubes = new GameObject[] {};

        public static AngryBirds _instance;
        
        public static ConfigEntry<bool> mEnabled;
        
        public ConfigDefinition mEnabledDef = new ConfigDefinition(pluginVersion, "Enable/Disable Mod");
        
        
        
        
        public override void enableMod(){
            mEnabled.Value = true;
            this.isEnabled = true;
        }
        public override void disableMod(){
            mEnabled.Value = false;
            this.isEnabled = false;
        }
        public override string getSettings(){
            return "";
        }
        public override void setSettings(string settings){
            
        }
        public AngryBirds(){
            
            mEnabled = Config.Bind(mEnabledDef, false, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes {Order = 0}));
        }
        void Awake(){
            _instance = this;
            this.repositoryUrl = null;
            this.isCheat = true;
            PolyTechMain.registerMod(this);
            Logger.LogInfo("Angry Birds Registered");
            Harmony.CreateAndPatchAll(typeof(AngryBirds));
            Logger.LogInfo("Angry Birds Methods Patched");
        }

        void Update(){
            if(!Bridge.m_Simulating){
                GlobalManager.controlState = ControlState.NON_SIM;
                return;
            }else if (GlobalManager.controlState == ControlState.NON_SIM){
                GlobalManager.controlState = ControlState.WAITING;
            }
            if(GlobalManager.controlState == ControlState.WAITING){
                if(Input.GetMouseButton(0)){
                    Logger.LogInfo("PULLING");
                    Logger.LogInfo("trying to draw a line");
                    GlobalManager.controlState = ControlState.PULLING;
                    PullManager.enter();
                    //slingshotLine.SetPositions(new Vector3[]{carStart, Input.mousePosition});
                }
            } else if (GlobalManager.controlState == ControlState.PULLING){
                if(!Input.GetMouseButton(0)){
                    PullManager.exit();
                    Logger.LogInfo("RELEASE");
                }else{
                    PullManager.tick();
                    /*
                    slingshotLine.SetPosition(1, carStart + .005f * (Input.mousePosition - pullStart));
                    PolyPhysics.Rigidbody[] bodies = (PolyPhysics.Rigidbody[])allBodies.GetValue((PolyPhysics.Vehicle)m_Physics.GetValue(Vehicles[0]));
                    foreach(PolyPhysics.Rigidbody body in bodies){
                        Poly.Physics.Solver.Motion vehicleMotion = ((Poly.Physics.Solver.Motion)motion.GetValue(body));
                        vehicleMotion.com = carStart + .005f * (Input.mousePosition - pullStart);
                        motion.SetValue(body,vehicleMotion);
                    }*/
                }
            }
            //Logger.LogInfo(GameStateManager.GetState());
            if(Bridge.m_Simulating){
                for(int i = 0; i <= Vehicles.GetUpperBound(0); i++){
                    VehicleCubes[i].transform.position = (Vector3) Vehicles[i].transform.position;
                    VehicleCubes[i].transform.rotation = (Quaternion) Vehicles[i].transform.rotation;
                }
                //Logger.LogInfo(Vehicles[Vehicles.GetUpperBound(0)].transform.position);
            }
        }
        [HarmonyPatch(typeof(Vehicles), "EnablePhysics")]
        [HarmonyPrefix]
        private static void EnableCars(ref bool __runOriginal){
            GlobalManager.vehicles = new Vehicle[] {};
            __runOriginal = true;
        }

        [HarmonyPatch(typeof(Vehicle), "SetUpSync")]
        [HarmonyPrefix]
        private static void VehicleAddedToSimulation(GameObject physicsVehicle, ref Vehicle __instance, ref bool __runOriginal){
            /*_instance.Logger.LogInfo("Vehicle Added to Simulation!");
            _instance.Logger.LogInfo(physicsVehicle);
            //__instance.m_MeshRenderer.enabled = false;
            VehicleObjects = VehicleObjects.Concat(new GameObject[] {physicsVehicle}).ToArray();
            Vehicles = Vehicles.Concat(new Vehicle[] {__instance}).ToArray();
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.SetActive(true);
            VehicleCubes = VehicleCubes.Concat(new GameObject[] {cube}).ToArray();
            __runOriginal = true;*/

            GlobalManager.vehicles = GlobalManager.vehicles.Concat(new Vehicle[] {__instance}).ToArray();
            __runOriginal = true;
        }

        [HarmonyPatch(typeof(Bridge), "StartSimulation")]
        [HarmonyPostfix]
        private static void StartSim(){
            GlobalManager.enter_sim();
        }

        [HarmonyPatch(typeof(GameStateSim), "Exit")]
        [HarmonyPostfix]
        private static void ExitSim(){
            GlobalManager.exit_sim();
        }

    }
}