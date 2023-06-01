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

public class DroneAnimation: MonoBehaviour
{
    public GameObject splash_screen;
    public InputField traj_folder;
    public InputField gate_folder;
    public LineRenderer line_render;
    public Camera main_camera;

    public DataReader data_reader;

    // 
    private bool static_camera;
    private List<Color> color_list;
    private List<Trajectory> trajectories;
    private List<LineRenderer> robot_line_renders;
    private List<LineRenderer> obj_line_renders;
    private float old_time;
    private Vector3 cam_offset;

    private Dictionary<string, ObjectState> robots;
    private Dictionary<string, ObjectState> static_objects;
    private Dictionary<string, ObjectState> dynamic_objects;

    // 
    private Dictionary<string, ObjectState> rpg_arena;

    // 
    private List<Trajectory> robot_trajs;
    private List<Trajectory> static_obj_trajs;
    private List<Trajectory> dynamic_obj_trajs;

    // Start is called before the first frame update
    void Start()
    {
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

      // 
      static_camera = true;
      old_time = Time.realtimeSinceStartup;
      cam_offset = new Vector3(-5.0f, 4.0f, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
      if (Input.GetKeyDown(KeyCode.Space))
      {
        reset();
      }

      float current_time = Time.realtimeSinceStartup;
      float time_diff = current_time - old_time;

      if (time_diff >= data_reader.config.sim_time) {
        old_time = Time.realtimeSinceStartup;

        // 
        int agent_idx = 0;
        foreach(Trajectory traj in robot_trajs) {
          string agent_name = "robot_" + agent_idx.ToString();
          if (traj.replay_idx < traj.length) {
            GameObject prefab = Resources.Load("Robots/" +traj.prefab_name) as GameObject;
            GameObject agent = data_reader.getGameobject(robots, agent_name, prefab);

            // 
            Vector3 position = traj.position[traj.replay_idx];
            Quaternion rotation = traj.rotation[traj.replay_idx];
            Vector3 size = traj.size[traj.replay_idx];
            agent.transform.SetPositionAndRotation(position, rotation);
            agent.transform.localScale = size;

            traj.replay_idx += 1;

            // simulate line rendering
            List<Vector3> positions = traj.getHistPositions();
            robot_line_renders[agent_idx].positionCount = positions.Count;
            robot_line_renders[agent_idx].SetPositions(positions.ToArray());
            robot_line_renders[agent_idx].startWidth = 0.01f; 
            robot_line_renders[agent_idx].endWidth = 0.1f; 
            robot_line_renders[agent_idx].material = new Material(Shader.Find("Sprites/Default"));
            robot_line_renders[agent_idx].startColor = color_list[agent_idx % color_list.Count]; 
            robot_line_renders[agent_idx].endColor = color_list[agent_idx % color_list.Count]; 

            if (agent_idx == 0 && !static_camera) {
              Vector3 newPos = agent.transform.position + cam_offset;
              main_camera.transform.position = Vector3.Slerp(main_camera.transform.position, newPos, 0.5f);
            }
          } else {
            splash_screen.gameObject.SetActive(true);
            reset();
          }
 
          agent_idx += 1;
        } 

        int dynamic_obj_idx = 0;
        foreach(Trajectory traj in dynamic_obj_trajs) {
          string agent_name = "dynamic_obj_" + dynamic_obj_idx.ToString();
          if (traj.replay_idx < traj.length) {
            GameObject prefab = Resources.Load("Objects/" + traj.prefab_name) as GameObject;
            GameObject agent = data_reader.getGameobject(dynamic_objects, agent_name, prefab);
            if (agent.GetComponent<Renderer>() != null)
            {
              agent.GetComponent<Renderer>().material.color = traj.color[traj.replay_idx];
            }

            // 
            Vector3 position = traj.position[traj.replay_idx];
            Quaternion rotation = traj.rotation[traj.replay_idx];
            Vector3 size = traj.size[traj.replay_idx];
            agent.transform.SetPositionAndRotation(position, rotation);
            agent.transform.localScale = size;

            traj.replay_idx += 1;

            // simulate line rendering
            List<Vector3> positions = traj.getHistPositions();
            // var line_render_i = line_renders[agent_idx];
            obj_line_renders[dynamic_obj_idx].positionCount = positions.Count;
            obj_line_renders[dynamic_obj_idx].SetPositions(positions.ToArray());
            obj_line_renders[dynamic_obj_idx].startWidth = 0.01f; 
            obj_line_renders[dynamic_obj_idx].endWidth = 0.1f; 
            obj_line_renders[dynamic_obj_idx].material = new Material(Shader.Find("Sprites/Default"));
            obj_line_renders[dynamic_obj_idx].startColor = traj.color[traj.replay_idx]; 
            obj_line_renders[dynamic_obj_idx].endColor =  traj.color[traj.replay_idx];
          } else {
            splash_screen.gameObject.SetActive(true);
            reset();
          }
 
          dynamic_obj_idx += 1;
        } 
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

            // add line render
            // LineRenderer line_render_i = ;
            robot_line_renders.Add(Instantiate(line_render));
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
            obj_line_renders.Add(Instantiate(line_render));
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

    public void Animation1() {
      static_camera = false;
      ReadButton();
      CreatRobots();
      VisualStaticObject();
      LoadRPGArena();
    }

    public void Animation2() {
      static_camera = true;
      ReadButton();
      CreatRobots();
      VisualStaticObject();
      LoadRPGArena();
    }

    public void CreatRobots() {
      for (int i=0; i < robot_trajs.Count; i ++ ) {
        Trajectory traj = robot_trajs[i]; 
        // 
        string obj_name = "robot_" + i.ToString();
        Debug.Log(obj_name);
        Color agent_color = color_list[i % color_list.Count];
        GameObject prefab = Resources.Load("Robots/" +data_reader.config.MyPrefabIDs.robot_prefab_id) as GameObject;
        // GameObject prefab = Resources.Load("Robots/" + traj.prefab_name) as GameObject;
        GameObject obj = data_reader.getGameobject(robots, obj_name, prefab);
 
        if (!static_camera) {
          main_camera.transform.position = traj.position[0] + cam_offset;
          main_camera.transform.eulerAngles = new Vector3(40, 90, 0);
        }
      }
    }

    public void CreatDynamicObject() {
      for (int i=0; i < dynamic_obj_trajs.Count; i ++ ) {
        Trajectory traj = dynamic_obj_trajs[i]; 
        // 
        string obj_name = "dynamic_obj_" + i.ToString();

        // GameObject prefab = Resources.Load("Objects/" + traj.dynamic_obj_prefabs) as GameObject;
        GameObject prefab = Resources.Load("Objects/" + traj.prefab_name) as GameObject;
        GameObject obj = data_reader.getGameobject(dynamic_objects, obj_name, prefab);
      }
    }


    public void VisualStaticObject(){
      for (int i=0; i< static_obj_trajs.Count; i++ ) {
          // 
          Trajectory traj = static_obj_trajs[i];
          int obj_i = 0;
          string obj_name = "object_" +  i.ToString();
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

    public void reset() {
      splash_screen.gameObject.SetActive(true);
      // foreach(ObjectState obj )
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
      for (int i = robot_line_renders.Count - 1; i >= 0; i--) {
        Destroy(robot_line_renders[i].gameObject);
      }
      for (int i = obj_line_renders.Count - 1; i >= 0; i--) {
        Destroy(obj_line_renders[i].gameObject);
      }
      for (int i = rpg_arena.Count - 1; i >= 0; i--) {
        var item = rpg_arena.ElementAt(i);
      }
      robot_line_renders = new List<LineRenderer>();
      obj_line_renders = new List<LineRenderer>();
      robots = new Dictionary<string, ObjectState>();
      static_objects = new Dictionary<string, ObjectState>();
      dynamic_objects = new Dictionary<string, ObjectState>();
      old_time = Time.realtimeSinceStartup;
      rpg_arena = new Dictionary<string, ObjectState>();

      // 
      robot_trajs = new List<Trajectory>();
      static_obj_trajs = new List<Trajectory>();
      dynamic_obj_trajs = new List<Trajectory>();
    }

    static public GameObject getChildGameObject(GameObject fromGameObject, string withName) {
         //Author: Isaac Dart, June-13.
         Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>(true);
         foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
         return null;
    }

}

}