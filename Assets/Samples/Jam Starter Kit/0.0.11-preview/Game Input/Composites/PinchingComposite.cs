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
    public class PinchingComposite : InputBindingComposite<float>
    {
        //Pixels of per-frame movement treated as full actuation. Scales EvaluateMagnitude only, never ReadValue.
        private const float k_FullActuationPixels = 50f;

        [InputControl(layout = "Touch")]
        public int firstTouch;
        [InputControl(layout = "Touch")]
        public int secondTouch;

        private struct TouchStateComparer : IComparer<TouchState>
        {
            public int Compare(TouchState x, TouchState y) => 1;
        }

        /// <summary>
        /// Reads the signed change in distance between the two touches since the previous frame, in
        /// screen pixels. Positive means the fingers spread apart, negative means they came together,
        /// zero means idle. Matches the impulse shape of a scroll wheel binding so both can drive the
        /// same action. Deliberately unscaled, because the consumer owns zoom sensitivity.
        /// </summary>
        /// <remarks>Rewritten by Claude (claude-opus-5) — 2026-08-15</remarks>
        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            var touch0 = context.ReadValue<TouchState, TouchStateComparer>(firstTouch);
            var touch1 = context.ReadValue<TouchState, TouchStateComparer>(secondTouch);

            if (!IsTouching(touch0.phase) || !IsTouching(touch1.phase))
                return 0f;

            var currentDistance = Vector2.Distance(touch0.position, touch1.position);
            var previousDistance = Vector2.Distance(
                touch0.position - touch0.delta,
                touch1.position - touch1.delta);

            return currentDistance - previousDistance;

            //Stationary counts: one finger can hold still while the other drives the whole gesture.
            static bool IsTouching(TouchPhase phase)
            {
                return phase is TouchPhase.Began or TouchPhase.Moved or TouchPhase.Stationary;
            }
        }

        /// <summary>
        /// Normalises the per-frame pixel delta into the 0-1 actuation range the Input System expects,
        /// so this binding reports a comparable magnitude to the other bindings on the same action.
        /// </summary>
        /// <remarks>Rewritten by Claude (claude-opus-5) — 2026-08-15</remarks>
        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            var value = ReadValue(ref context);

            return Mathf.Clamp01(Mathf.Abs(value) / k_FullActuationPixels);
        }

        static PinchingComposite() => InputSystem.RegisterBindingComposite<PinchingComposite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
        } // Trigger static constructor.
    }
}
