//Based on: https://discussions.unity.com/t/implementing-pinching/902137/5

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameInput.Composites
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [DisplayStringFormat("{firstTouch}+{secondTouch}")]
    public class RotateComposite : InputBindingComposite<float>
    {
        [InputControl(layout = "Touch")] 
        public int firstTouch;
        [InputControl(layout = "Touch")] 
        public int secondTouch;
    
        private struct TouchStateComparer : IComparer<TouchState>
        {
            public int Compare(TouchState x, TouchState y) => 1;
        }

        // This method computes the resulting input value of the composite based
        // on the input from its part bindings.
        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            TouchState touch0 = context.ReadValue<TouchState, TouchStateComparer>(firstTouch);
            TouchState touch1 = context.ReadValue<TouchState, TouchStateComparer>(secondTouch);

            if (touch0.phase != TouchPhase.Moved || touch1.phase != TouchPhase.Moved)
                return 0f;

            Vector2 line1 = touch0.startPosition - touch1.startPosition;
            Vector2 line2 = touch0.position - touch1.position;

            return Vector2.SignedAngle(line2, line1);
        }

        // This method computes the current actuation of the binding as a whole.
        // This is used to evaluate the Action start and canceled callbacks. 
        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            return Mathf.InverseLerp(0, 180, Mathf.Abs(ReadValue(ref context)));
        }

        static RotateComposite() => InputSystem.RegisterBindingComposite<RotateComposite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() { } // Trigger static constructor.
    }
}