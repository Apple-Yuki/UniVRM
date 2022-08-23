using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniVRM10
{
    /// <summary>
    /// このクラスのヒエラルキーが 正規化された TPose を表している。
    /// 同時に、元のヒエラルキーの初期回転を保持する。
    /// Apply 関数で、再帰的に正規化済みのローカル回転から初期回転を加味したローカル回転を作って適用する。
    /// </summary>
    public class Vrm10BoneWithAxis
    {
        public readonly HumanBodyBones Bone;

        /// <summary>
        /// 元のヒエラルキーの対応ボーン
        /// </summary>
        public readonly Transform Target;

        /// <summary>
        /// 回転と拡大縮小を除去した(正規化された)ボーン。
        /// このボーンに対して localRotation を代入する。
        /// </summary>
        public readonly Transform Normalized;

        /// <summary>
        /// 元のボーンの初期回転。
        /// </summary>
        public readonly Quaternion InitialLocalRotation;

        public readonly Quaternion ToLocal;

        public List<Vrm10BoneWithAxis> Children = new List<Vrm10BoneWithAxis>();

        public Vrm10BoneWithAxis(Transform current, Quaternion parentInverse, HumanBodyBones bone)
        {
            if (bone == HumanBodyBones.LastBone)
            {
                throw new ArgumentNullException();
            }
            if (current == null)
            {
                throw new ArgumentNullException();
            }
            Bone = bone;
            Target = current;
            Normalized = new GameObject(bone.ToString()).transform;
            Normalized.position = current.position;
            // InitialLocalRotation = parentInverse * current.rotation;
            InitialLocalRotation = current.localRotation;
            // InitialLocalRotation = current.rotation;
            ToLocal = current.rotation;
        }

        public static Vrm10BoneWithAxis Build(UniHumanoid.Humanoid humanoid, Dictionary<HumanBodyBones, Vrm10BoneWithAxis> boneMap)
        {
            var hips = new Vrm10BoneWithAxis(humanoid.Hips, Quaternion.identity, HumanBodyBones.Hips);

            foreach (Transform child in humanoid.Hips)
            {
                Traverse(humanoid, child, hips, boneMap);
            }

            return hips;
        }

        private static void Traverse(UniHumanoid.Humanoid humanoid, Transform current, Vrm10BoneWithAxis parent, Dictionary<HumanBodyBones, Vrm10BoneWithAxis> boneMap)
        {
            if (humanoid.TryGetBoneForTransform(current, out var bone))
            {

                // ヒューマンボーンだけを対象にするので、
                // parent が current の直接の親でない場合がある。
                // ワールド回転 parent^-1 * current からローカル回転を算出する。
                var parentInverse = Quaternion.Inverse(parent.Target.rotation);

                var newBone = new Vrm10BoneWithAxis(current, parentInverse, bone);
                newBone.Normalized.SetParent(parent.Normalized, true);
                parent.Children.Add(newBone);
                parent = newBone;
                boneMap.Add(bone, newBone);
            }

            foreach (Transform child in current)
            {
                Traverse(humanoid, child, parent, boneMap);
            }
        }

        /// <summary>
        /// 親から再帰的にNormalized の ローカル回転を初期回転を加味して Target に適用する。
        /// </summary>
        public void ApplyRecursive(Quaternion worldParentRotation)
        {
            // var pose = InitialLocalRotation * Normalized.localRotation * Quaternion.Inverse(InitialLocalRotation);
            // var pose = Quaternion.Inverse(InitialLocalRotation) * Normalized.localRotation * InitialLocalRotation;
            Target.localRotation = InitialLocalRotation * Quaternion.Inverse(ToLocal) * Normalized.localRotation * ToLocal;
            // Target.localRotation = InitialLocalRotation * Normalized.localRotation; // * Quaternion.Inverse(InitialLocalRotation);
            // Target.localRotation = InitialLocalRotation;

            foreach (var child in Children)
            {
                child.ApplyRecursive(Normalized.rotation);
            }
        }
    }
}
