using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

using System.IO;
using System.Globalization;


namespace FlightmareAnimation
{

    public class ObjectState {

      public bool initialized { get; set; } = false;
      public GameObject gameObj { get; set; }
      public GameObject template { get; set; }

      public ObjectState(GameObject template)
      {
        this.gameObj = GameObject.Instantiate(template);
        this.template = template;
      }
    }

    public class Trajectory {
      public List<Vector3> position { get; set; }
      public List<Quaternion> rotation { get; set; }
      public List<Vector3> velocity { get; set; }
      public List<Vector3> size {get; set;} 
      public List<float> current_t {get; set;} 

      // 
      public List<Vector4> color {get; set;}
      public string prefab_name;

      public int replay_idx;
      public int length;

      public Trajectory() {
        position = new List<Vector3>();
        rotation = new List<Quaternion>();
        velocity = new List<Vector3>();
        size = new List<Vector3>();
        current_t = new List<float>();

        color= new List<Vector4>();

        // 
        replay_idx = 0;
        length = 0;
      }

      public List<Vector3> getHistPositions(){
        if (replay_idx >= length) replay_idx = length-1;

        int hist_length = 20;
        int start_idx = 0;
        if (replay_idx >= hist_length)
          start_idx = replay_idx - hist_length;
        // 
        List<Vector3> positions = new List<Vector3>();


        for (int i=start_idx; i < replay_idx; i++) {
          positions.Add(position[i]);
        }
        return positions;
      }
    }

    public class DataReader {

      public ConfigData config = new ConfigData();


      // Get Wrapper object, defaulting to a passed in template if it does not exist.
      public ObjectState  getWrapperObject(Dictionary<string, ObjectState> objects, string ID, GameObject template)
      {
        if (!objects.ContainsKey(ID))
        {
          objects[ID] = new ObjectState(template);
        }
        return objects[ID];
      }

      public GameObject getGameobject(Dictionary<string, ObjectState> objects, string ID, GameObject template)
      {
          return getWrapperObject(objects, ID, template).gameObj;
      }

      public void InitConfig() {
        string json = File.ReadAllText(Application.streamingAssetsPath + "/data.json");
        Debug.Log(json);
        config = JsonUtility.FromJson<ConfigData>(json);

        if (!config.DataFolder.use_data_folder) {
          config.DataFolder.traj_folder = Application.streamingAssetsPath + "/DroneTrajs";
          config.DataFolder.static_obj_folder = Application.streamingAssetsPath + "/StaticObjs";
          config.DataFolder.dynamic_obj_folder = Application.streamingAssetsPath + "/DynamicObjs";
        }
        config.color_csv = Application.streamingAssetsPath + "/color_list.csv";
      }

      // read the color list
      public List<Color> ReadColorList() {
        List<Color> color_list = new List<Color>();
        StreamReader str_reader = new StreamReader(config.color_csv);
        int color_cnt = 0;

        while(true) {
            string data_string = str_reader.ReadLine();
            if (data_string == null) {
              break;
            }

            color_cnt += 1;
            if (color_cnt <= 1 ) {
              continue;
            }

            string[] data_values = data_string.Split(',');
            float color_r = float.Parse(data_values[1], CultureInfo.InvariantCulture.NumberFormat);
            float color_g = float.Parse(data_values[2], CultureInfo.InvariantCulture.NumberFormat);
            float color_b = float.Parse(data_values[3], CultureInfo.InvariantCulture.NumberFormat);
            float color_a = float.Parse(data_values[4], CultureInfo.InvariantCulture.NumberFormat);
            // 
            Color new_color = new Color(color_r, color_g, color_b, color_a);
            color_list.Add(new_color);
        }
        return color_list;
      }

      // read object
      public Trajectory ReadStaticObject(string obj_csv) {
        StreamReader str_reader = new StreamReader(obj_csv);
        bool end_of_file = false;
        int obj_cnt = 0;

        Trajectory trajectory = new Trajectory();

        // 
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(obj_csv);
        Debug.Log(fileNameWithoutExt);
        string prefab_name = fileNameWithoutExt.Substring(0, fileNameWithoutExt.LastIndexOf("_"));
        trajectory.prefab_name = prefab_name;

        while(!end_of_file) {
          string data_string = str_reader.ReadLine();
          if (data_string == null) {
            end_of_file = true;
            break;
          }

          obj_cnt += 1;
          if (obj_cnt <= 1  || obj_cnt % 5 == 0) {
            continue;
          }

          string[] data_values = data_string.Split(',');

          // 
          Vector3 position;
          position.x = float.Parse(data_values[1], CultureInfo.InvariantCulture.NumberFormat);
          position.y = float.Parse(data_values[3], CultureInfo.InvariantCulture.NumberFormat);
          position.z = float.Parse(data_values[2], CultureInfo.InvariantCulture.NumberFormat);

          // 
          Quaternion rotation;
          rotation.w = float.Parse(data_values[4], CultureInfo.InvariantCulture.NumberFormat);
          rotation.x = -float.Parse(data_values[5], CultureInfo.InvariantCulture.NumberFormat);
          rotation.y = -float.Parse(data_values[7], CultureInfo.InvariantCulture.NumberFormat);
          rotation.z = -float.Parse(data_values[6], CultureInfo.InvariantCulture.NumberFormat);

          // 
          Vector3 size;
          size.x = float.Parse(data_values[8], CultureInfo.InvariantCulture.NumberFormat);
          size.y = float.Parse(data_values[10], CultureInfo.InvariantCulture.NumberFormat);
          size.z = float.Parse(data_values[9], CultureInfo.InvariantCulture.NumberFormat);

          Vector4 color;
          color.x = float.Parse(data_values[11], CultureInfo.InvariantCulture.NumberFormat);
          color.y = float.Parse(data_values[12], CultureInfo.InvariantCulture.NumberFormat);
          color.z = float.Parse(data_values[13], CultureInfo.InvariantCulture.NumberFormat);
          color.w = float.Parse(data_values[14], CultureInfo.InvariantCulture.NumberFormat);
          
          Vector3 velocity = new Vector3(0.0f, 0.0f, 0.0f);

          trajectory.current_t.Add(0.0f);
          trajectory.position.Add(position);
          trajectory.rotation.Add(rotation);
          trajectory.velocity.Add(velocity);
          trajectory.size.Add(size);

          // 
          trajectory.color.Add(color);

          trajectory.length += 1;
        }

        return trajectory;
      }

      public Trajectory ReadRobotTraj(string object_csv)
      {
        StreamReader str_reader = new StreamReader(object_csv);
        bool end_of_file = false;

        // 
        int obj_cnt = 0;

        Trajectory trajectory = new Trajectory();

        // 
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(object_csv);
        // string prefab_name = fileNameWithoutExt.Substring(0, fileNameWithoutExt.LastIndexOf("_"));
        trajectory.prefab_name = "RacingDrone";

        while(!end_of_file) {
          string data_string = str_reader.ReadLine();
          if (data_string == null) {
            end_of_file = true;
            break;
          }

          obj_cnt += 1;
          if (obj_cnt <= 1 || obj_cnt % 5 == 0) {
            continue;
          }

          string[] data_values = data_string.Split(',');

          // 
          float current_t;
          current_t = float.Parse(data_values[config.start_idx + 0], CultureInfo.InvariantCulture.NumberFormat);

          Vector3 position;
          position.x = float.Parse(data_values[config.start_idx + 1], CultureInfo.InvariantCulture.NumberFormat);
          position.y = float.Parse(data_values[config.start_idx + 3], CultureInfo.InvariantCulture.NumberFormat);
          position.z = float.Parse(data_values[config.start_idx + 2], CultureInfo.InvariantCulture.NumberFormat);

          // 
          Quaternion rotation;
          rotation.w = float.Parse(data_values[config.start_idx + 4], CultureInfo.InvariantCulture.NumberFormat);
          rotation.x = -float.Parse(data_values[config.start_idx + 5], CultureInfo.InvariantCulture.NumberFormat);
          rotation.y = -float.Parse(data_values[config.start_idx + 7], CultureInfo.InvariantCulture.NumberFormat);
          rotation.z = -float.Parse(data_values[config.start_idx + 6], CultureInfo.InvariantCulture.NumberFormat);

          // 
          Vector3 velocity;
          velocity.x = float.Parse(data_values[config.start_idx + 8], CultureInfo.InvariantCulture.NumberFormat);
          velocity.y = float.Parse(data_values[config.start_idx + 10], CultureInfo.InvariantCulture.NumberFormat);
          velocity.z = float.Parse(data_values[config.start_idx + 9], CultureInfo.InvariantCulture.NumberFormat);

          // 
          trajectory.current_t.Add(0.0f);
          trajectory.position.Add(position);
          trajectory.rotation.Add(rotation);
          trajectory.velocity.Add(velocity);
          Vector3 size = new Vector3(1.0f, 1.0f, 1.0f);
          trajectory.size.Add(size);
          trajectory.length += 1;
        }
        return trajectory;

      }

    }
}
