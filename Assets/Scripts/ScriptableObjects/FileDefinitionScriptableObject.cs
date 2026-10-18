using ldjam60.Files;
using UnityEngine;

namespace ldjam60.ScriptableObjects
{
    [CreateAssetMenu(fileName = "FileDefinition", menuName = "ScriptableObjects/File Definition", order = 1)]
    public class FileDefinitionScriptableObject : ScriptableObject
    {
        public FILE_TYPE FileType => fileType;
        public string FileName => fileName;
        public Sprite Image => image;
        public string FileContents => fileContents;
        public int ProgressTowardsObjective => progressTowardsObjective;
        
        //TODO Need to add public accessors
        [SerializeField]
        private FILE_TYPE fileType;
        
        [SerializeField]
        private string fileName;
        [SerializeField, Min(0)]
        private int progressTowardsObjective;
        
        [Header("Contents")]
        [SerializeField, TextArea]
        private string fileContents;
        [SerializeField]
        private Sprite image;
    }
}