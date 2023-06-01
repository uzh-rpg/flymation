using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

using System.IO;
using System.Globalization;
using UnityEngine.SceneManagement;

namespace FlightmareAnimation
{

public class DroneSnapshot : MonoBehaviour
{
    public GameObject splash_screen;
    public LineRenderer line_render;
    public DataReader data_reader;

    // 
    private List<Color> color_list;
    private List<LineRenderer> robot_line_renders;
    private List<LineRenderer> obj_line_renders;

    // 
    private Dictionary<string, ObjectState> robots;
    private Dictionary<string, ObjectState> static_objects;
    private Dictionary<string, ObjectState> dynamic_objects;

    // 
    private Dictionary<string, ObjectState> rpg_arena;

    // 
    private List<Trajectory> robot_trajs;
    private List<Trajectory> static_obj_trajs;
    private List<Trajectory> dynamic_obj_trajs;


    // 
    Gradient gradient;
    GradientColorKey[] colorKey;
    GradientAlphaKey[] alphaKey;


    // Start is called before the first frame update
    void Start() {
      color_list = new List<Color>();

      robots = new Dictionary<string, ObjectState>();
      robot_line_renders = new List<LineRenderer>();
      obj_line_renders = new List<LineRenderer>();
      static_objects = new Dictionary<string, ObjectState>(); 
      dynamic_objects = new Dictionary<string, ObjectState>(); 
      rpg_arena = new Dictionary<string, ObjectState>(); 

      data_reader = new DataReader();

      // 
      robot_trajs = new List<Trajectory>();
      static_obj_trajs = new List<Trajectory>();
      dynamic_obj_trajs = new List<Trajectory>();

      DontDestroyOnLoad(this.gameObject);
    }

    // Update is called once per frame
    void Update() {
      if (Input.GetKeyDown(KeyCode.Space))
      {
        reset();
      }
    }

    public void ReadButton() {

      // ---------------------------------------------------
      // ============= initialize configurations ==================
      // ---------------------------------------------------
      data_reader.InitConfig();

      // ---------------------------------------------------
      // ============= load color csv ==================
      // ---------------------------------------------------
      bool valid_color_csv = false;
      if (!string.IsNullOrEmpty(data_reader.config.color_csv)) {
        color_list = data_reader.ReadColorList();
        valid_color_csv = true;
      }

      // ---------------------------------------------------
      // ============= load robot trajectory csv ==================
      // ---------------------------------------------------
      bool valid_traj_folder = false;
      if (!string.IsNullOrEmpty(data_reader.config.DataFolder.traj_folder)) {
        DirectoryInfo traj_dir = new DirectoryInfo(data_reader.config.DataFolder.traj_folder);
        if (traj_dir.Exists) {
          FileInfo[] info = traj_dir.GetFiles("*.csv");
          int traj_cnt = 0;
          foreach (FileInfo f in info) { 
            Trajectory traj = data_reader.ReadRobotTraj(f.FullName); 
            robot_trajs.Add(traj);
            traj_cnt += 1;
          }
          // 
          valid_traj_folder = true;
        }
      }

      // ---------------------------------------------------
      // ============= load object csv ==================
      // ---------------------------------------------------
      bool valid_obj_folder = false;
      if (!string.IsNullOrEmpty(data_reader.config.DataFolder.static_obj_folder)) {
        DirectoryInfo obj_dir = new DirectoryInfo(data_reader.config.DataFolder.static_obj_folder);
        if (obj_dir.Exists) {
          FileInfo[] obj_info = obj_dir.GetFiles("*.csv");
          foreach (FileInfo g in obj_info) { 
            Trajectory traj = data_reader.ReadStaticObject(g.FullName); 
            static_obj_trajs.Add(traj);
          }
          // 
          valid_obj_folder = true;
        }
      }

      // ---------------------------------------------------
      // ============= load dynamic object csv ==================
      // ---------------------------------------------------
      if (!string.IsNullOrEmpty(data_reader.config.DataFolder.dynamic_obj_folder)) {
        DirectoryInfo obj_dir = new DirectoryInfo(data_reader.config.DataFolder.dynamic_obj_folder);
        if (obj_dir.Exists) {
          FileInfo[] obj_info = obj_dir.GetFiles("*.csv");
          foreach (FileInfo g in obj_info) { 
            Trajectory traj = data_reader.ReadStaticObject(g.FullName); 
            dynamic_obj_trajs.Add(traj);
          }
        }
      }

     if (valid_traj_folder || valid_obj_folder || valid_color_csv) {
        if (splash_screen != null && splash_screen.activeSelf) {
              splash_screen.gameObject.SetActive(false);
        }
     }
    }

    public void LoadRPGArena() {
      if (data_reader.config.RPGArena) {
          GameObject prefab = Resources.Load("Scenes/" + "RPGArena") as GameObject;
          GameObject obj = data_reader.getGameobject(rpg_arena, "RPGArena", prefab);
      }
    }

    public void SnapShot1() {
      ReadButton();
      // visualize robot trajectory only (without robot)
      VisualRobotTraj(false);
      VisualDynamicObjTraj(false);
      VisualStaticObject();
      LoadRPGArena();
    }

    public void SnapShot2() {
      ReadButton();
      // visualize robot trajectory and robot
      VisualRobotTraj(true);
      VisualDynamicObjTraj(true);
      VisualStaticObject();
      LoadRPGArena();
    }

    static public GameObject getChildGameObject(GameObject fromGameObject, string withName) {
         //Author: Isaac Dart, June-13.
         Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>(true);
         foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
         return null;
    }

    public void defineColorGradient() {
      gradient = new Gradient();

      // Populate the color keys at the relative time 0 and 1 (0 and 100%)
      colorKey = new GradientColorKey[2];
      colorKey[0].color = Color.red;
      colorKey[0].time = 0.0f;
      colorKey[1].color = Color.blue;
      colorKey[1].time = 1.0f;

      // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
      alphaKey = new GradientAlphaKey[2];
      alphaKey[0].alpha = 1.0f;
      alphaKey[0].time = 0.0f;
      alphaKey[1].alpha = 0.0f;
      alphaKey[1].time = 1.0f;

      gradient.SetKeys(colorKey, alphaKey);
    }

    public void VisualRobotTraj(bool show_all_object) {
      for (int i=0; i < robot_trajs.Count; i ++ ) {
        Trajectory traj = robot_trajs[i];
        
        int traj_idx = 1;
        if(show_all_object) { 
          traj_idx = traj.length;
        }
        for(int obj_i=traj.length - 1; obj_i >=traj.length - traj_idx; obj_i--) {
          // 
          string obj_name = "robot_" + i.ToString() + "_" + obj_i.ToString();
          GameObject prefab = Resources.Load("Robots/" + traj.prefab_name) as GameObject;
          GameObject obj = data_reader.getGameobject(robots, obj_name, prefab);

          obj.transform.SetPositionAndRotation(traj.position[obj_i], traj.rotation[obj_i]);
          obj.transform.localScale = traj.size[obj_i];
        }

        List<Vector3> positions = traj.position;
        LineRenderer line_render_i = Instantiate(line_render);
        line_render_i.positionCount = positions.Count;
        line_render_i.SetPositions(positions.ToArray());
        line_render_i.startWidth = 0.01f; 
        line_render_i.endWidth = 0.1f; 
        line_render_i.material = new Material(Shader.Find("Sprites/Default"));
        Color traj_color = color_list[i % color_list.Count];
        line_render_i.startColor = traj_color; 
        line_render_i.endColor = traj_color; 

        robot_line_renders.Add(line_render_i);
      }
    }

    public void VisualDynamicObjTraj(bool show_all_object) {
      for (int i=0; i < dynamic_obj_trajs.Count; i ++ ) {
        Trajectory traj = dynamic_obj_trajs[i];
        
        int traj_idx = 1;
        if(show_all_object) { 
          traj_idx = traj.length;
        }
        for(int obj_i=traj.length - 1; obj_i >=traj.length - traj_idx; obj_i--) {
          // 
          string obj_name = "dynamic_obj_" + i.ToString() + "_" + obj_i.ToString();
          GameObject prefab = Resources.Load("Objects/" + traj.prefab_name) as GameObject;
          GameObject obj = data_reader.getGameobject(robots, obj_name, prefab);

          if (obj.GetComponent<Renderer>() != null)
          {
            obj.GetComponent<Renderer>().material.color = traj.color[obj_i];
          }

          obj.transform.SetPositionAndRotation(traj.position[obj_i], traj.rotation[obj_i]);
          obj.transform.localScale = traj.size[obj_i];
        }

        List<Vector3> positions = traj.position;
        LineRenderer line_render_i = Instantiate(line_render);
        line_render_i.positionCount = positions.Count;
        line_render_i.SetPositions(positions.ToArray());
        line_render_i.startWidth = 0.01f; 
        line_render_i.endWidth = 0.1f; 
        line_render_i.material = new Material(Shader.Find("Sprites/Default"));
        Color traj_color = traj.color[0];
        line_render_i.startColor = traj_color; 
        line_render_i.endColor = traj_color; 

        obj_line_renders.Add(line_render_i);
      }
    }

    public void VisualStaticObject(){
      for (int i=0; i< static_obj_trajs.Count; i++ ) {
          string info = "Number of trajs " + static_obj_trajs.Count + ", Number of Objs: " + static_obj_trajs[i].length;
          Debug.Log(info);

          // 
          Trajectory traj = static_obj_trajs[i];
          for(int obj_i=0; obj_i < traj.length; obj_i++) {
            // create objects
            string obj_name = "object_" +  i.ToString() + "_" + obj_i.ToString();
            GameObject prefab = Resources.Load("Objects/" + traj.prefab_name) as GameObject;
            GameObject obj = data_reader.getGameobject(static_objects, obj_name, prefab);

            if (obj.GetComponent<Renderer>() != null)
            {
              obj.GetComponent<Renderer>().material.color = traj.color[obj_i];
            }

            // set position, orientation, and size
            obj.transform.SetPositionAndRotation(traj.position[obj_i], traj.rotation[obj_i]);
            obj.transform.localScale = traj.size[obj_i];
        }
      }
    }

    public void reset() { 
      splash_screen.gameObject.SetActive(true);
      for (int i = robots.Count - 1; i >= 0; i--) {
        var item = robots.ElementAt(i);
        // var itemKey = item.Key;
        Destroy(item.Value.gameObj);
      }
      for (int i = static_objects.Count - 1; i >= 0; i--) {
        var item = static_objects.ElementAt(i);
        // var itemKey = item.Key;
        Destroy(item.Value.gameObj);
      }
      for (int i = dynamic_objects.Count - 1; i >= 0; i--) {
        var item = dynamic_objects.ElementAt(i);
        Destroy(item.Value.gameObj);
      }
      for (int i = rpg_arena.Count - 1; i >= 0; i--) {
        var item = rpg_arena.ElementAt(i);
        // var itemKey = item.Key;
        Destroy(item.Value.gameObj);
      }
      for (int i = robot_line_renders.Count - 1; i >= 0; i--) {
        Destroy(robot_line_renders[i].gameObject);
      }
      for (int i = obj_line_renders.Count - 1; i >= 0; i--) {
        Destroy(obj_line_renders[i].gameObject);
      }
      robot_line_renders = new List<LineRenderer>();
      obj_line_renders = new List<LineRenderer>();
      robots = new Dictionary<string, ObjectState>();
      static_objects = new Dictionary<string, ObjectState>();
      dynamic_objects = new Dictionary<string, ObjectState>();
      rpg_arena = new Dictionary<string, ObjectState>();

      // 
      robot_trajs = new List<Trajectory>();
      static_obj_trajs = new List<Trajectory>();
      dynamic_obj_trajs = new List<Trajectory>();
    }

}

}

// public void chagneColor(GameObject obj, float color_gradient, Color traj_color) {
//   List<string> obj_components = new List<string>
//       {"Battery_Parent", "FL_Motor_Parent", "RL_Motor_Parent", "FR_Motor_Parent", "RR_Motor_Parent", "Cam_Parent"};
  
//   var frame = getChildGameObject(obj, "Racing Drone Merged");
//   var obj_renderer = frame.GetComponent<Renderer>();

//   // float alpha = gradient.Evaluate(color_gradient);
//   Color start_color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
//   Color new_color = Color.Lerp(start_color, traj_color, color_gradient);
//   obj_renderer.material.SetColor("_Color", new_color);

//   // 
//   foreach (var cmp in obj_components){
//     var cmp_obj = getChildGameObject(frame, cmp);
//     var cmp_obj_renderer = cmp_obj.GetComponent<Renderer>();
//     cmp_obj_renderer.material.SetColor("_Color", new_color);

//     if (cmp == "FL_Motor_Parent") {
//       var prop_obj = getChildGameObject(cmp_obj, "Prop");
//       var prop_obj_renderer = prop_obj.GetComponent<Renderer>();
//       prop_obj_renderer.material.SetColor("_Color", new_color);
//     }
//     if (cmp == "FR_Motor_Parent") {
//       var prop_obj = getChildGameObject(cmp_obj, "Prop");
//       var prop_obj_renderer = prop_obj.GetComponent<Renderer>();
//       prop_obj_renderer.material.SetColor("_Color", new_color);
//     }
//     if (cmp == "RR_Motor_Parent") {
//       var prop_obj = getChildGameObject(cmp_obj, "Prop");
//       var prop_obj_renderer = prop_obj.GetComponent<Renderer>();
//       prop_obj_renderer.material.SetColor("_Color", new_color);
//     }
//     if (cmp == "RL_Motor_Parent") {
//       var prop_obj = getChildGameObject(cmp_obj, "Prop");
//       var prop_obj_renderer = prop_obj.GetComponent<Renderer>();
//       prop_obj_renderer.material.SetColor("_Color", new_color);
//     }
//   }
// }
