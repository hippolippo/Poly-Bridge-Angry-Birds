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

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    
    public class AngryBirds : PolyTechMod {

        public static Vector3 pullStart;
        public static FieldInfo allBodies = typeof(PolyPhysics.Vehicle).GetField("allBodies", BindingFlags.NonPublic | BindingFlags.Instance);
        public static FieldInfo m_Physics = typeof(Vehicle).GetField("m_Physics", BindingFlags.NonPublic | BindingFlags.Instance);

        public static FieldInfo motion = typeof(PolyPhysics.Rigidbody).GetField("motion", BindingFlags.NonPublic | BindingFlags.Instance);

        public static ControlState controlState = ControlState.NON_SIM;
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
                controlState = ControlState.NON_SIM;
                return;
            }else if (controlState == ControlState.NON_SIM){
                controlState = ControlState.WAITING;
            }
            if(controlState == ControlState.WAITING){
                if(Input.GetMouseButton(0)){
                    Logger.LogInfo("PULLING");
                    controlState = ControlState.PULLING;
                    pullStart = Input.mousePosition;
                }
            } else if (controlState == ControlState.PULLING){
                if(!Input.GetMouseButton(0)){
                    Logger.LogInfo("RELEASE");
                    controlState = ControlState.MOVING;
                    Vector3 restoring3 = pullStart - Input.mousePosition;
                    Vector2 restoring2 = new Vector2(restoring3.x, restoring3.y);
                    PolyPhysics.Rigidbody[] bodies = (PolyPhysics.Rigidbody[])allBodies.GetValue((PolyPhysics.Vehicle)m_Physics.GetValue(Vehicles[0]));
                    foreach(PolyPhysics.Rigidbody body in bodies){
                        Poly.Physics.Solver.Motion vehicleMotion = ((Poly.Physics.Solver.Motion)motion.GetValue(body));
                        vehicleMotion.linVel = restoring2 * .001f;
                        motion.SetValue(body,vehicleMotion);
                    }
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
            VehicleObjects = new GameObject[] {};
            Vehicles = new Vehicle[] {};
            foreach(GameObject obj in VehicleCubes){
                Destroy(obj);
            }
            VehicleCubes = new GameObject[] {};
            __runOriginal = true;
        }

        [HarmonyPatch(typeof(Vehicle), "SetUpSync")]
        [HarmonyPrefix]
        private static void VehicleAddedToSimulation(GameObject physicsVehicle, ref Vehicle __instance, ref bool __runOriginal){
            _instance.Logger.LogInfo("Vehicle Added to Simulation!");
            _instance.Logger.LogInfo(physicsVehicle);
            __instance.m_MeshRenderer.enabled = false;
            VehicleObjects = VehicleObjects.Concat(new GameObject[] {physicsVehicle}).ToArray();
            Vehicles = Vehicles.Concat(new Vehicle[] {__instance}).ToArray();
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.SetActive(true);
            VehicleCubes = VehicleCubes.Concat(new GameObject[] {cube}).ToArray();
            __runOriginal = true;
        }

    }
}