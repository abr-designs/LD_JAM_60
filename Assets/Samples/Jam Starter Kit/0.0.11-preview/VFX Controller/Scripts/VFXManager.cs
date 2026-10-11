using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

namespace VisualFX
{
    internal class VFXManager : HiddenSingleton<VFXManager>
    {
        //============================================================================================================//
        [Serializable]
        private class VfxData
        {
            [SerializeField]
            internal string name;
            public VFX type;
            public GameObject prefab;
            [Min(0f)]
            public float lifetime;
        }

        //============================================================================================================//

        [SerializeField]
        private VfxData[] vfxDatas;

        private Dictionary<VFX, VfxData> _vfxDataDictionary;
        
        //Unity Functions
        //============================================================================================================//

        // Start is called before the first frame update
        private void Start()
        {
            InitVfxLibrary();
        }
        
        //============================================================================================================//

        private void InitVfxLibrary()
        {
            var count = vfxDatas.Length;
            _vfxDataDictionary = new Dictionary<VFX, VfxData>(count);
            for (var i = 0; i < count; i++)
            {
                var vfxData = vfxDatas[i];
                _vfxDataDictionary.Add(vfxData.type, vfxData);
            }
        }
        
        //============================================================================================================//

        public static GameObject PlayAtLocation(VFX vfx, Vector3 worldPosition, float scale = 1f, bool keepAlive = false)
        {
            Assert.IsNotNull(Instance, $"Missing the {nameof(VFXManager)} in the Scene!!");
            
            return Instance._PlayAtLocation(vfx, worldPosition, scale, keepAlive);
        }
        //This is meant to be called via the VFXExtensions class
        private GameObject _PlayAtLocation(VFX vfx, Vector3 worldPosition, float scale, bool keepAlive)
        {
            var vfxData = GetVFXData(vfx);

            var instance = Instantiate(vfxData.prefab, worldPosition, Quaternion.identity, transform);
            
            if(scale != 1f)
                instance.transform.localScale = Vector3.one * scale;

            if(keepAlive == false)
                //Destroy the VFX after its set lifetime
                Destroy(instance, vfxData.lifetime);
            
            return instance;
        }

        private VfxData GetVFXData(VFX vfx)
        {
            if (_vfxDataDictionary.TryGetValue(vfx, out var vfxData) == false)
                return null;
            
            Assert.IsNotNull(vfxData);
            Assert.IsNotNull(vfxData.prefab);

            return vfxData;
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            for (int i = 0; i < vfxDatas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(vfxDatas[i].name))
                    vfxDatas[i].name = vfxDatas[i].type.ToString();
            }
            
            UnityEditor.EditorUtility.SetDirty(this);

            var enumTypes = (VFX[])Enum.GetValues(typeof(VFX));

            foreach (var enumType in enumTypes)
            {
                Assert.IsTrue(vfxDatas.Count(x => x.type == enumType) <= 1,
                    $"<b><color=\"red\">ERROR</color></b>\nMore than 1 VFX found in the VFX manager for {enumType}. <color=\"red\"><b>CAN ONLY HAVE 1</b></color>");
            }

        }

#endif

        
        //============================================================================================================//



    }
    

}
