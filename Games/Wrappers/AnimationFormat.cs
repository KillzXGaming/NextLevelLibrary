using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.Animations;
using OpenTK;

namespace NextLevelLibrary
{
    public class AnimationFormat : STAnimation
    {
        /// <summary>
        /// Gets the active skeleton visbile in the scene that may be used for animation.
        /// </summary>
        /// <returns></returns>
        public STSkeleton GetActiveSkeleton()
        {
            foreach (var container in Runtime.ModelContainers)
            {
                var skel = container.SearchActiveSkeleton();
                if (skel != null)
                    return skel;
            }
            return null;
        }

        public override void NextFrame()
        {
            base.NextFrame();

            bool update = false;
            var skeleton = GetActiveSkeleton();

            if (skeleton == null) return;

            foreach (var group in AnimGroups)
            {
                if (group is AnimationGroup)
                {
                    var boneAnim = (AnimationGroup)group;
                    var bone = skeleton.SearchBone(boneAnim.Name);

                    if (bone == null)
                        continue;

                    update = true;

                    Vector3 position = bone.Position;
                    Vector3 scale = bone.Scale;

                    if (boneAnim.TranslateX.HasKeys)
                        position.X = boneAnim.TranslateX.GetFrameValue(Frame) * ModelWrapper.PreviewScale;
                    if (boneAnim.TranslateY.HasKeys)
                        position.Y = boneAnim.TranslateY.GetFrameValue(Frame) * ModelWrapper.PreviewScale;
                    if (boneAnim.TranslateZ.HasKeys)
                        position.Z = boneAnim.TranslateZ.GetFrameValue(Frame) * ModelWrapper.PreviewScale;

                    if (boneAnim.ScaleX.HasKeys)
                        scale.X = boneAnim.ScaleX.GetFrameValue(Frame);
                    if (boneAnim.ScaleY.HasKeys)
                        scale.Y = boneAnim.ScaleY.GetFrameValue(Frame);
                    if (boneAnim.ScaleZ.HasKeys)
                        scale.Z = boneAnim.ScaleZ.GetFrameValue(Frame);

                    bone.AnimationController.Position = position;
                    bone.AnimationController.Scale = scale;

                    if (boneAnim.UseQuaternion)
                    {
                        Quaternion rotation = bone.Rotation;

                        if (boneAnim.RotateX.HasKeys)
                            rotation.X = boneAnim.RotateX.GetFrameValue(Frame);
                        if (boneAnim.RotateY.HasKeys)
                            rotation.Y = boneAnim.RotateY.GetFrameValue(Frame);
                        if (boneAnim.RotateZ.HasKeys)
                            rotation.Z = boneAnim.RotateZ.GetFrameValue(Frame);
                        if (boneAnim.RotateW.HasKeys)
                            rotation.W = boneAnim.RotateW.GetFrameValue(Frame);

                        bone.AnimationController.Rotation = rotation;
                    }
                    else
                    {
                        Vector3 rotationEuluer = bone.EulerRotation;

                        if (boneAnim.RotateX.HasKeys)
                            rotationEuluer.X = boneAnim.RotateX.GetFrameValue(Frame);
                        if (boneAnim.RotateY.HasKeys)
                            rotationEuluer.Y = boneAnim.RotateY.GetFrameValue(Frame);
                        if (boneAnim.RotateZ.HasKeys)
                            rotationEuluer.Z = boneAnim.RotateZ.GetFrameValue(Frame);

                        bone.AnimationController.EulerRotation = rotationEuluer;
                    }
                }
            }

            if (update)
            {
                skeleton.Update();
            }
        }
    }

    public class AnimationGroup : STAnimGroup
    {
        public bool UseQuaternion { get; set; } = true;

        public AnimationTrack TranslateX = new AnimationTrack();
        public AnimationTrack TranslateY = new AnimationTrack();
        public AnimationTrack TranslateZ = new AnimationTrack();

        public AnimationTrack RotateX = new AnimationTrack();
        public AnimationTrack RotateY = new AnimationTrack();
        public AnimationTrack RotateZ = new AnimationTrack();
        public AnimationTrack RotateW = new AnimationTrack();

        public AnimationTrack ScaleX = new AnimationTrack();
        public AnimationTrack ScaleY = new AnimationTrack();
        public AnimationTrack ScaleZ = new AnimationTrack();

        public AnimationTrack TexCoordU = new AnimationTrack();
        public AnimationTrack TexCoordV = new AnimationTrack();

        public override List<STAnimationTrack> GetTracks()
        {
            List<STAnimationTrack> tracks = new List<STAnimationTrack>();
            tracks.Add(TranslateX);
            tracks.Add(TranslateY);
            tracks.Add(TranslateZ);
            tracks.Add(RotateX);
            tracks.Add(RotateY);
            tracks.Add(RotateZ);
            tracks.Add(RotateW);
            tracks.Add(ScaleX);
            tracks.Add(ScaleY);
            tracks.Add(ScaleZ);
            tracks.Add(TexCoordU);
            tracks.Add(TexCoordV);
            return tracks;
        }
    }

    public class AnimationTrack : STAnimationTrack
    {
        public AnimationTrack() {
            InterpolationType = STInterpoaltionType.Linear;
        }

        public void AddKey(float frame, float value) {
            KeyFrames.Add(new STKeyFrame(frame, value));
        }
    }
}
