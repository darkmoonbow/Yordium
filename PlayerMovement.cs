using System;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;
using Stride.Core.Mathematics;
using System.Xml.Linq;
using Stride.Animations;
using System.IO;
using Stride.UI.Controls;
using LiteNetLib.Utils;
using LiteNetLib;
using Microsoft.VisualBasic.Logging;
using Stride.Core.Diagnostics;

namespace Yordium1
{
    public class PlayerMovement : SyncScript
    {
        public float Speed = 6f;
        public float JumpStrength = 5f;
        public Entity CameraEntity;
        public UIPage UIPanel;

        private CharacterComponent character;
        private Quaternion lastRotation; // store last facing rotation

        private AnimationComponent animationComponent;

        private readonly Logger log = GlobalLogger.GetLogger("Network");
        


        public override void Start()
        {
            character = Entity.Get<CharacterComponent>();
            animationComponent = Entity.Get<AnimationComponent>();
            lastRotation = Entity.Transform.Rotation;
        }

        protected void PlayAnimation(string name)
        {
            if (!animationComponent.IsPlaying(name))
                animationComponent.Play(name);
        }

        public override void Update()
        {
            if (character == null)
                return;
            EditText messageInput = new EditText();
            foreach (var child in UIPanel.RootElement.VisualChildren)
            {

                if (child is EditText editText)
                {
                    messageInput = editText;


                }
            }


            Vector3 inputDir = Vector3.Zero;
            
            if (!messageInput.IsCaretVisible) {
                if (Input.IsKeyDown(Keys.W)) inputDir.Z += 1;
                if (Input.IsKeyDown(Keys.S)) inputDir.Z -= 1;
                if (Input.IsKeyDown(Keys.A)) inputDir.X -= 1;
                if (Input.IsKeyDown(Keys.D)) inputDir.X += 1;
            } else
            {
                var serverPeer = GameClient.Instance?.ServerPeer;
                var PlayerInfo = GameClient.Instance?.PlayerInfo;
                if (Input.IsKeyDown(Keys.Enter) && serverPeer != null && messageInput.Text != null)
                {

                    var writer = new NetDataWriter();
                    writer.Put("CHAT");
                    writer.Put(PlayerInfo.Id);
                    writer.Put(messageInput.Text);
                    serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                    
                    log.Info("Sent CHAT packet");
                    
                }
            }

            Vector3 moveDir = Vector3.Zero;

            if (CameraEntity != null)
            {
                var camForward = CameraEntity.Transform.WorldMatrix.Forward;
                var camRight = CameraEntity.Transform.WorldMatrix.Right;
                camForward.Y = 0;
                camRight.Y = 0;
                camForward.Normalize();
                camRight.Normalize();

                moveDir = camForward * inputDir.Z + camRight * inputDir.X;

                if (moveDir.LengthSquared() > 0.01f)
                {
                    Vector3 lookDir = new Vector3(moveDir.X, 0, moveDir.Z);
                    lookDir.Normalize();

                    Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.UnitY) * Quaternion.RotationY(MathUtil.DegreesToRadians(90));
                    Entity.Transform.Rotation = targetRot;
                    lastRotation = targetRot; // remember last rotation



                    //PlayAnimation("Walk");


                }
                else
                {
                    // Keep last facing direction when not moving
                    Entity.Transform.Rotation = lastRotation;
                }
            }

            // Apply movement
            var currentVel = character.LinearVelocity;
            var newVel = new Vector3(moveDir.X * Speed, currentVel.Y, moveDir.Z * Speed);
            character.SetVelocity(newVel);

            // Jump
            if (Input.IsKeyPressed(Keys.Space))
            {
                character.Jump();
            }
        }
    }
}
