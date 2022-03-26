using System;
using System.Dynamic;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using System.Linq;
using System.Numerics;
using System.Globalization;

namespace GameServer 
{
    class SceneParser 
    {
        public List<RectCollider> ParseScene(string path)
        {
            string file = File.ReadAllText(path);
            Dictionary<string, dynamic> objects = ParseFiles(file);
            List <dynamic> transforms = GetTransforms(objects);
            return TransfromToCollider(transforms);
        }

        private dynamic ParseYaml(string s)
        {
            var deserializer = new DeserializerBuilder().Build();
            return deserializer.Deserialize<ExpandoObject>(s);
        }

        private Dictionary<string, dynamic> ParseFiles(string file)
        {
            string[] files_array = file.Split("--- ");
            List<string> files = files_array.ToList<string>();
            files.RemoveAt(0);

            
            Dictionary<string, dynamic> objects = new Dictionary<string, dynamic>();
            foreach (var f in files)
            {
                string[] splits = f.Split(Environment.NewLine, 2);
                string header = splits[0];
                string document = splits[1];
                string object_id = header.Split("&")[1];

                objects[object_id] = ParseYaml(document);
            }

            return objects;
        }

        private List<dynamic> GetTransforms(Dictionary<string, dynamic> objects){
            List<dynamic> transforms = new List<dynamic>();
            foreach (KeyValuePair<string, dynamic> element in objects)
            {
                string type = ((IDictionary<String, Object>) element.Value).Keys.First();

                if (type == "GameObject")
                {
                    dynamic gameObject = element.Value.GameObject;

                    if (gameObject["m_TagString"] != "ServerCollideable")
                    {
                        continue;
                    }

                    foreach (var component in gameObject["m_Component"])
                    {
                        string fileID = component["component"]["fileID"];
                        dynamic component_object = objects[fileID];
                        string component_type = ((IDictionary<String, Object>) component_object).Keys.First();

                        if (component_type == "Transform"){
                            transforms.Add(component_object.Transform);
                        }
                    }
                }
            }
            return transforms;
        }

        private List<RectCollider> TransfromToCollider(List <dynamic> transforms)
        {
            List<RectCollider> colliders = new List<RectCollider>();
            foreach (var transform in transforms)
            {
                if (float.Parse(transform["m_LocalRotation"]["x"]) != 0 || 
                    float.Parse(transform["m_LocalRotation"]["y"]) != 0 || 
                    float.Parse(transform["m_LocalRotation"]["z"]) != 0)
                    {
                        Console.WriteLine($"[Import Error]: A GameObject Transform had a rotation associated with it. This is not supported. The object was ignored");
                        continue;
                    }

                colliders.Add(new RectCollider(
                    new Vector2(float.Parse(transform["m_LocalPosition"]["x"], CultureInfo.InvariantCulture), float.Parse(transform["m_LocalPosition"]["y"], CultureInfo.InvariantCulture)),
                    new Vector2(float.Parse(transform["m_LocalScale"]["x"],    CultureInfo.InvariantCulture), float.Parse(transform["m_LocalScale"]["y"],    CultureInfo.InvariantCulture))
                ));
            }

            return colliders;
        }
    }
}