using System;
using UnityEngine;

namespace WotHCustomItemSDK
{
    internal sealed class CustomBagRuntimeState : MonoBehaviour
    {
        public Component BagCategoryBar3;
        public Component BagCategoryBar4;

        public GameObject CustomType;
        public GameObject CustomPackageType;
        public GameObject CustomSelectType;
        public GameObject CustomPackageSelectType;

        public bool Initialized;

        public void ResetState()
        {
            BagCategoryBar3 = null;
            BagCategoryBar4 = null;
            CustomType = null;
            CustomPackageType = null;
            CustomSelectType = null;
            CustomPackageSelectType = null;
            Initialized = false;
        }
    }
}
