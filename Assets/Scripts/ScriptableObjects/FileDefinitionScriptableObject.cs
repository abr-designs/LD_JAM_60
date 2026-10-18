using ldjam60.Files;
using UnityEngine;

namespace ldjam60.ScriptableObjects
{
    [CreateAssetMenu(fileName = "FileDefinition", menuName = "ScriptableObjects/File Definition", order = 1)]
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