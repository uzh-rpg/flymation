using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

using System.IO;
using System.Globalization;

namespace FlightmareAnimation
{
    [System.Serializable]
    public class ConfigData {
      [SerializeField] public bool RPGArena;
      [SerializeField] public string color_csv;
      [SerializeField] public int start_idx;
      [SerializeField] public float sim_time = 0;
      [SerializeField]  public DataFolder DataFolder;
      [SerializeField]  public MyPrefabIDs MyPrefabIDs;
      [SerializeField]  public CameraConfig CameraConfig;
    }

    [System.Serializable]
    public class MyPrefabIDs 
    {
     [SerializeField] public string robot_prefab_id;
    }

    [System.Serializable]
    public class DataFolder
    {
      [SerializeField] public bool use_data_folder;
      [SerializeField] public string traj_folder;
      [SerializeField] public string static_obj_folder;
      [SerializeField] public string dynamic_obj_folder;
    }

    // Camera class for decoding the ZMQ messages.
    [System.Serializable]
    public class CameraConfig
    {
      [SerializeField] public string ID { get; set; }
      // // Metadata
      [SerializeField] public int channels { get; set; }
      [SerializeField] public int width { get; set; }
      [SerializeField] public int height { get; set; }
      [SerializeField] public float fov { get; set; }
    }

    public class Lidar_t
    {
      public string ID { get; set; }
      public int num_beams;
      public float max_distance;
      public float start_angle;
      public float end_angle;
      // transformation matrix with respect to center
      // of the vehicle
      public List<float> T_BS { get; set; }
    }

    public class Vehicle_t
    {
      public string ID { get; set; }
      public List<float> position { get; set; }
      public List<float> rotation { get; set; }
      public List<float> size { get; set; }
      public List<CameraConfig> cameras { get; set; }
      public List<Lidar_t> lidars;
      public bool hasCollisionCheck = true;
      public bool hasVehicleCollision = false;
    }

    // Generic object class for decoding the ZMQ messages.
    public class Object_t
    {
      public string ID { get; set; }
      public string prefabID { get; set; }
      public List<float> position { get; set; }
      public List<float> rotation { get; set; }
      // Metadata
      public List<float> size { get; set; }
    }
}