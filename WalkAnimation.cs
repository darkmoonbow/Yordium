using Stride.Engine;
using Stride.Core.Mathematics;
using System;
using Stride.Input;


namespace Yordium1 {
    public class WalkAnimation : SyncScript
    {
        public Entity LeftArm, RightArm, LeftLeg, RightLeg;
        public float WalkSpeed = 5f;
    
        private float time = 0f;
    
        public override void Update()
        {
            var moving = Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.A) ||
                         Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.D);
    
            if (moving)
            {
                time += (float)Game.UpdateTime.Elapsed.TotalSeconds * WalkSpeed;
                float angle = (float)Math.Sin(time) * 0.5f;
    
                if (LeftArm != null) LeftArm.Transform.Rotation = Quaternion.RotationX(angle);
                if (RightArm != null) RightArm.Transform.Rotation = Quaternion.RotationX(-angle);
                if (LeftLeg != null) LeftLeg.Transform.Rotation = Quaternion.RotationX(-angle);
                if (RightLeg != null) RightLeg.Transform.Rotation = Quaternion.RotationX(angle);
            }
        }
    }

}

