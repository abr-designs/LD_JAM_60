using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
    public abstract class BaseUIWindow : MonoBehaviour
    {
        [SerializeField, Header("Window Settings")]
        private bool shouldStartOpen;

        [SerializeField]
        private Button closeButton;
        
        protected void Start()
        {
            Assert.IsNotNull(closeButton);
            
            closeButton.onClick.AddListener(CloseWindow);
            
            if(shouldStartOpen)
                OpenWindow();
            else
                CloseWindow();
        }

        public virtual void OpenWindow()
        {
            gameObject.SetActive(true);
        }
        
        public virtual void CloseWindow()
        {
            gameObject.SetActive(false);
        }
    }
}