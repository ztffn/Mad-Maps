using System;
using System.Collections.Generic;
using UnityEngine;
using FullSerializer;

namespace MadMaps.Common.Serialization
{	
	public class fsUnityObjectConverter : fsConverter {

		public override bool CanProcess(Type type){
			return typeof(UnityEngine.Object).IsAssignableFrom(type);
		}

		public override bool RequestCycleSupport(Type storageType){
			return false;
		}

		public override bool RequestInheritanceSupport(Type storageType){
			return false;
		}

		public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType){
			
			var database = Serializer.Context.Get<List<UnityEngine.Object>>();
			var o = instance as UnityEngine.Object;

			var index = -1;
			for (var i = 0; i < database.Count; i++){
				if (ReferenceEquals(database[i], o)){
					index = i;
					break;
				}
			}

			//this is done to avoid serializing 0 because it's default value of int and will not be printed,
			//which is done for performance. Thus we always start from index 1.
			if (database.Count == 0){
				database.Add(null);
			}

			if (index <= 0){
				index = database.Count;
				database.Add(o);
			}

			serialized = new fsData(index);
			return fsResult.Success;
		}

		public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType){
			var database = Serializer.Context.Get<List<UnityEngine.Object>>();
			var index = (int)data.AsInt64;
			
			if (index >= database.Count)
				return fsResult.Warn("A Unity Object reference has not been deserialized");
			
			instance = database[index];
			return fsResult.Success;
		}

		public override object CreateInstance(fsData data, Type storageType){
			return null;
		}
	}

    ///Serializes/Deserializes to/from JSON with 'FullSerializer'
    public static class JSONSerializer {
        
#if UNITY_EDITOR //this is used to avoid calling Unity API in serialization for the editor
        [UnityEditor.InitializeOnLoad]
        class StartUp{
            static StartUp(){
                //set to false since this is always called in editor start.
                JSONSerializer.applicationPlaying = false;
                UnityEditor.EditorApplication.playmodeStateChanged += ()=>{ JSONSerializer.applicationPlaying = Application.isPlaying; };
            }
        }
#endif

        private static Dictionary<string, fsData> cache = new Dictionary<string, fsData>();
        private static object serializerLock = new object();
        private static fsSerializer serializer = new fsSerializer();
        private static bool init = false;

        //initialize to true since StartUp is editor only
        public static bool applicationPlaying = true;

        ///Serialize to json
        public static string Serialize(Type type, object value, bool pretyJson = false, List<UnityEngine.Object> objectReferences = null) {

            lock (serializerLock)
            {
                if (!init){
                    serializer.AddConverter(new fsUnityObjectConverter());
                    init = true;
                }

                //set the objectReferences context
                if (objectReferences != null){
                    serializer.Context.Set<List<UnityEngine.Object>>(objectReferences);
                }

                //serialize the data
                fsData data;
                serializer.TrySerialize(type, value, out data).AssertSuccess();

                cache[fsJsonPrinter.CompressedJson(data)] = data;

                //print data to json
                if (pretyJson){
                    return fsJsonPrinter.PrettyJson(data);
                }
                return fsJsonPrinter.CompressedJson(data);
            }
        }

        ///Deserialize generic
        public static T Deserialize<T>(string serializedState, List<UnityEngine.Object> objectReferences = null){
            return (T)Deserialize(typeof(T), serializedState, objectReferences);
        }

        ///Deserialize from json
        public static object Deserialize(Type type, string serializedState, List<UnityEngine.Object> objectReferences = null) {

            lock (serializerLock)
            {
                if (!init){
                    serializer.AddConverter(new fsUnityObjectConverter());
                    init = true;
                }

                if (objectReferences != null){
                    serializer.Context.Set<List<UnityEngine.Object>>(objectReferences);
                }

                fsData data = null;
                cache.TryGetValue(serializedState, out data);
                if (data == null){
                    data = fsJsonParser.Parse(serializedState);
                    cache[serializedState] = data;                
                }

                //deserialize the data
                object deserialized = null;
                serializer.TryDeserialize(data, type, ref deserialized).AssertSuccess();

                return deserialized;
            }
        }

    }
}