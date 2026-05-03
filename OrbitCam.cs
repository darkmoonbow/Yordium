using System;
using Stride.Engine;
using Stride.Input;
using Stride.Core.Mathematics;

namespace Yordium1
{
    public class OrbitCam : SyncScript
    {
        public Entity Target;
        public float Distance = 6f;
        public float Sensitivity = 300f;
        public float Pitch = 0.3f;
        public float Yaw = 0f;

        private bool isDragging = false;

        public override void Start()
        {
            Game.Window.IsMouseVisible = true;
        }

        public override void Update()
        {
            if (Target == null)
                return;

            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                if (!isDragging)
                {
                    isDragging = true;
                    Input.LockMousePosition(true);
                    Game.Window.IsMouseVisible = false;
                }

                var delta = Input.MouseDelta;
                Yaw -= delta.X * Sensitivity * (float)Game.UpdateTime.Elapsed.TotalSeconds;
                Pitch -= -delta.Y * Sensitivity * (float)Game.UpdateTime.Elapsed.TotalSeconds;
                Pitch = MathUtil.Clamp(Pitch, -1.2f, 1.2f);
            }
            else if (isDragging)
            {
                isDragging = false;
                Input.UnlockMousePosition();
                Game.Window.IsMouseVisible = true;
            }

            float scroll = Input.MouseWheelDelta;
            if (Math.Abs(scroll) > 0.01f)
            {
                Distance -= scroll * 2f;
                Distance = MathUtil.Clamp(Distance, 6f, 24f);
            }

            Vector3 offset;
            offset.X = Distance * (float)Math.Sin(Yaw) * (float)Math.Cos(Pitch);
            offset.Y = Distance * (float)Math.Sin(Pitch);
            offset.Z = Distance * (float)Math.Cos(Yaw) * (float)Math.Cos(Pitch);

            Vector3 targetPos = Target.Transform.WorldMatrix.TranslationVector;
            Vector3 camPos = targetPos + offset;
            Entity.Transform.Position = camPos;

            Vector3 forward = Vector3.Normalize(targetPos - camPos);
            Quaternion rotation = Quaternion.LookRotation(-forward, Vector3.UnitY);
            Entity.Transform.Rotation = rotation;
        }
    }
}
