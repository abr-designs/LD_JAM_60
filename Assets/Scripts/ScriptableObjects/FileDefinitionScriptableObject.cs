using Files;
using UnityEngine;

namespace ScriptableObjects
{
    public class FileDefinitionScriptableObject : ScriptableObject
    {
        //TODO Need to add public accessors
        [SerializeField]
        private FILE_TYPE fileType;
        
        [SerializeField]
        private string fileName;
        [SerializeField, Min(0)]
        private int progressTowardsObjective;
        
        [Header("Contents")]
        [SerializeField]
        private string fileContents;
        [SerializeField]
        private Sprite image;
    }
}