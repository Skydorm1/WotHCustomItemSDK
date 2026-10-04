using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace WotHCustomItemSDK
{
    internal static class CustomBagCategoryPatch
    {
        private static Type _panelType;
        private static Type _bagCategoryBarType;
        private static Type _uiTabViewType;
        private static Type _tabButtonType;
        private static Type _bagManagerType;
        private static Type _eventManagerType;
        private static Type _managerBaseGenericType;
        private static Type _imageType;
        private static Type _buttonType;
        private static Type _tagSlotType;
        private static Type _localizeType;

        private static readonly string[] SelectCustomClassifications =
        {
            "CustomSelectType/AllCustoms",
            "CustomSelectType/Materials",
            "CustomSelectType/Furnitures",
            "CustomSelectType/Electrical",
            "CustomSelectType/Plant",
            "CustomSelectType/Clothing",
            "CustomSelectType/Decoration",
            "CustomSelectType/Hygiene",
            "CustomSelectType/Food",
            "CustomSelectType/Other"
        };

        private static string[] GetCustomPackageClassifications()
        {
            List<string> result = new List<string>();
            result.Add("CustomPackageSelectType/AllCustomsPackage");

            foreach (string packageName in PackageRegistry.PackageNames)
            {
                if (!string.IsNullOrWhiteSpace(packageName))
                    result.Add("CustomPackageSelectType/" + packageName);
            }

            return result.ToArray();
        }

        private sealed class ItemRename
        {
            public readonly string OriginalName;
            public readonly string NewName;

            public ItemRename(string originalName, string newName)
            {
                OriginalName = originalName;
                NewName = newName;
            }
        }

        private static readonly ItemRename[] MaterialsItems =
        {
            new ItemRename("窗户", "Window"),
            new ItemRename("隔断", "Partition"),
            new ItemRename("栏杆", "Railing"),
            new ItemRename("墙壁", "Wall"),
            new ItemRename("地块", "Tile"),
            new ItemRename("楼梯", "Stairs"),
            new ItemRename("门", "Door")
        };

        private static readonly ItemRename[] FurnituresItems =
        {
            new ItemRename("床", "Bed"),
            new ItemRename("柜子", "Cabinet"),
            new ItemRename("椅子", "Chair"),
            new ItemRename("沙发", "Sofa"),
            new ItemRename("架子", "StorageRack"),
            new ItemRename("茶几", "CoffeeTable"),
            new ItemRename("卫浴", "Bathroom"),
            new ItemRename("厨房台面", "KitchenCountertop"),
            new ItemRename("桌子", "Table"),
            new ItemRename("镜子", "Mirror")
        };

        private static readonly ItemRename[] ElectricalItems =
        {
            new ItemRename("数码", "Digital"),
            new ItemRename("家电", "Appliance"),
            new ItemRename("灯具", "Lighting")
        };

        private static readonly ItemRename[] PlantItems =
        {
            new ItemRename("落地植物", "FloorPlant"),
            new ItemRename("壁挂植物", "WallMountedPlant"),
            new ItemRename("种植工具", "PlantingTool"),
            new ItemRename("种子", "Seed")
        };

        private static readonly ItemRename[] ClothingItems =
        {
            new ItemRename("衣服", "Clothes"),
            new ItemRename("鞋子", "Shoes"),
            new ItemRename("饰品", "Accessory")
        };

        private static readonly ItemRename[] DecorationItems =
        {
            new ItemRename("纪念品", "Collectibles"),
            new ItemRename("摆件", "Figurine"),
            new ItemRename("地毯", "Carpet"),
            new ItemRename("书", "Book"),
            new ItemRename("工具", "Tool"),
            new ItemRename("兴趣爱好", "Hobby"),
            new ItemRename("墙上装饰", "WallDecor"),
            new ItemRename("装饰", "Decor")
        };

        private static readonly ItemRename[] FoodItems =
        {
            new ItemRename("餐具", "Tableware"),
            new ItemRename("饮料", "Drinks"),
            new ItemRename("食物", "Food"),
            new ItemRename("厨具", "Coolware"),
            new ItemRename("调料", "Seasoning")
        };

        private static readonly ItemRename[] HygieneItems =
        {
            new ItemRename("清洁", "CleaningSupplies"),
            new ItemRename("个人护理", "PersonalCare"),
            new ItemRename("健康", "HealthCare")
        };

        private static readonly ItemRename[] OtherItems =
        {
            new ItemRename("庭院", "Courtyard"),
            new ItemRename("垃圾桶", "TrashBin"),
            new ItemRename("收纳", "Storage")
        };

        private static readonly string[] RenamedItemLocalizationNames =
        {
            "Window", "Partition", "Railing", "Wall", "Tile", "Stairs", "Door",
            "Bed", "Cabinet", "Chair", "Sofa", "StorageRack", "CoffeeTable",
            "Bathroom", "KitchenCountertop", "Table", "Mirror",
            "Digital", "Appliance", "Lighting",
            "FloorPlant", "WallMountedPlant", "PlantingTool", "Seed",
            "Clothes", "Shoes", "Accessory",
            "Collectibles", "Figurine", "Carpet", "Book", "Tool", "Hobby",
            "WallDecor", "Decor",
            "Tableware", "Drinks", "Food", "Coolware", "Seasoning",
            "CleaningSupplies", "PersonalCare", "HealthCare",
            "Courtyard", "TrashBin", "Storage"
        };

        internal static void Initialize()
        {
            _panelType = ResolveRequired("UnLimitBagPanel");
            _bagCategoryBarType = ResolveRequired("BagCategoryBar");
            _uiTabViewType = ResolveRequired("UITabView");
            _tabButtonType = ResolveRequired("TabButton");
            _bagManagerType = ResolveRequired("BagManager");
            _eventManagerType = ResolveRequired("EventManager");
            _managerBaseGenericType = ResolveRequired("ManagerBase`1");
            _imageType = ResolveRequired("UnityEngine.UI.Image");
            _buttonType = ResolveRequired("UnityEngine.UI.Button");
            _tagSlotType = ReflectionUtil.TypeByName("TagSlot");
            _localizeType = ResolveRequired("I2.Loc.Localize");
        }

        internal static void Apply(Harmony harmony)
        {
            PatchOnEnable(harmony);
            PatchStart(harmony);
            PatchSelectTypeChange(harmony);
            PatchClickBar(harmony, "ClickBar1");
            PatchClickBar(harmony, "ClickBar2");
            PatchClickBar(harmony, "ClickBar3");

            MethodInfo onDestroy = AccessTools.Method(_panelType, "OnDestroy");
            if (onDestroy != null)
            {
                harmony.Patch(
                    onDestroy,
                    postfix: new HarmonyMethod(typeof(CustomBagCategoryPatch), nameof(OnDestroyPostfix))
                );
            }
        }

        private static Type ResolveRequired(string name)
        {
            Type type = ReflectionUtil.TypeByName(name);
            if (type == null)
                throw new InvalidOperationException("Required game type not found: " + name);

            return type;
        }

        private static void PatchOnEnable(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(_panelType, "OnEnable");
            if (target == null)
            {
                Plugin.LogWarning("OnEnable was not found; falling back to Start for custom object creation.");
                return;
            }

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CustomBagCategoryPatch), nameof(OnEnablePrefix))
            );
        }

        private static void PatchStart(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(_panelType, "Start");
            if (target == null)
                throw new MissingMethodException(_panelType.FullName, "Start");

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CustomBagCategoryPatch), nameof(StartPrefix)),
                transpiler: new HarmonyMethod(typeof(CustomBagCategoryPatch), nameof(StartTranspiler))
            );
        }

        private static void PatchSelectTypeChange(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(_panelType, "SelectTypeChange");
            if (target == null)
                throw new MissingMethodException(_panelType.FullName, "SelectTypeChange");

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CustomBagCategoryPatch), nameof(SelectTypeChangePrefix))
            );
        }

        private static void PatchClickBar(Harmony harmony, string methodName)
        {
            MethodInfo target = AccessTools.Method(_panelType, methodName);
            if (target == null)
            {
                Plugin.LogWarning("Click bar method not found: " + methodName);
                return;
            }

            harmony.Patch(
                target,
                postfix: new HarmonyMethod(typeof(CustomBagCategoryPatch), nameof(ClickBarPostfix))
            );
        }

        private static void OnEnablePrefix(object __instance)
        {
            CustomItemService.UnlockAllCustomItems();

            StartPrefix(__instance);
        }

        private static void StartPrefix(object __instance)
        {
            Component panel = __instance as Component;
            if (panel == null)
                return;

            try
            {
                SetupCustomObjects(panel);
            }
            catch (Exception ex)
            {
                Plugin.LogError("SetupCustomObjects failed: " + ex);
            }
        }

        private static IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool inserted = false;
            int bagCategoryBarOnInitCalls = 0;
            MethodInfo initializeCustomBars = AccessTools.Method(
                typeof(CustomBagCategoryPatch),
                nameof(InitializeCustomBars)
            );

            List<CodeInstruction> list = new List<CodeInstruction>(instructions);

            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction instruction = list[i];
                yield return instruction;

                MethodBase calledMethod = instruction.operand as MethodBase;
                if (calledMethod == null)
                    continue;

                if (calledMethod.Name != "OnInit")
                    continue;

                Type declaringType = calledMethod.DeclaringType;
                if (declaringType == null || !declaringType.Name.Contains("BagCategoryBar"))
                    continue;

                bagCategoryBarOnInitCalls++;
                if (!inserted && bagCategoryBarOnInitCalls == 3)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, initializeCustomBars);
                    inserted = true;
                }
            }

            if (!inserted)
            {
                throw new InvalidOperationException(
                    "Could not find the third BagCategoryBar.OnInit call in UnLimitBagPanel.Start(). " +
                    "The game's Start() method likely differs from the supplied Original.cs."
                );
            }
        }

        private static void InitializeCustomBars(object __instance)
        {
            Component panel = __instance as Component;
            if (panel == null)
                return;

            CustomBagRuntimeState state = GetState(panel);
            if (state == null)
                return;

            InitBagCategoryBar(
                state.BagCategoryBar3,
                SelectCustomClassifications,
                panel
            );

            InitBagCategoryBar(
                state.BagCategoryBar4,
                GetCustomPackageClassifications(),
                panel
            );
        }

        private static CustomBagRuntimeState GetState(Component panel)
        {
            if (panel == null)
                return null;

            CustomBagRuntimeState state = panel.GetComponent<CustomBagRuntimeState>();
            if (state == null)
                state = panel.gameObject.AddComponent<CustomBagRuntimeState>();

            return state;
        }

        private static void SetupCustomObjects(Component panel)
        {
            CustomBagRuntimeState state = GetState(panel);
            if (state == null || state.Initialized)
                return;

            state.Initialized = true;

            Component bagCategoryBar = GetComponentField(panel, "bagCategoryBar");
            Component bagCategoryBar1 = GetComponentField(panel, "bagCategoryBar_1");
            Component bagCategoryBar2 = GetComponentField(panel, "bagCategoryBar_2");

            if (bagCategoryBar == null || bagCategoryBar1 == null || bagCategoryBar2 == null)
            {
                state.Initialized = false;
                throw new InvalidOperationException("Original BagCategoryBar references are missing.");
            }

            SetupSingleItemCategory(panel, state, bagCategoryBar, bagCategoryBar1);
            SetupPackageCategory(panel, state, bagCategoryBar, bagCategoryBar2);
        }

        private static void SetupSingleItemCategory(
            Component panel,
            CustomBagRuntimeState state,
            Component bagCategoryBar,
            Component sourceBar)
        {
            GameObject bagCat3 = ReflectionUtil.InstantiateChild(
                sourceBar.gameObject,
                bagCategoryBar.transform.parent
            );

            if (bagCat3 == null)
                throw new InvalidOperationException("Could not clone BagCategoryBar (3).");

            Component bar3 = bagCat3.GetComponent(_bagCategoryBarType);
            state.BagCategoryBar3 = bar3;
            bagCat3.name = "BagCategoryBar (3)";

            GameObject originalSelectThing = GetGameObjectField(sourceBar, "selectThing");
            if (originalSelectThing != null)
                SetGameObjectField(bar3, "selectThing", originalSelectThing);

            GameObject originalSelectType = GetGameObjectField(sourceBar, "selectType");
            GameObject clonedSelectType = CloneGameObject(originalSelectType, "CustomType");
            state.CustomType = clonedSelectType;
            SetGameObjectField(bar3, "selectType", clonedSelectType);

            GameObject originalSelectThingType = GetGameObjectField(sourceBar, "selectThingType");
            GameObject clonedSelectThingType = CloneGameObject(originalSelectThingType, "CustomSelectType");
            state.CustomSelectType = clonedSelectThingType;
            SetGameObjectField(bar3, "selectThingType", clonedSelectThingType);

            GameObject contentObj = RequireChild(clonedSelectType, "Content");
            Component contentTabView = contentObj.GetComponent(_uiTabViewType);
            if (contentTabView == null)
                throw new InvalidOperationException("CustomType/Content has no UITabView.");

            SetMemberRequired(bar3, "uITabView", contentTabView);

            IList tabs = GetTabs(contentTabView);
            ReflectionUtil.RemoveAtSafe(tabs, 2);
            ReflectionUtil.RemoveAtSafe(tabs, 1);
            ClearClassificationPages(bar3);

            RenameRequired(clonedSelectType, "全部", "AllCustoms");
            ReflectionUtil.DestroyIfFound(clonedSelectType.transform, "收藏");
            ReflectionUtil.DestroyIfFound(clonedSelectType.transform, "方块");
            RenameRequired(clonedSelectType, "建材", "Materials");
            RenameRequired(clonedSelectType, "家具", "Furnitures");
            RenameRequired(clonedSelectType, "电器", "Electrical");
            RenameRequired(clonedSelectType, "植物", "Plant");
            RenameRequired(clonedSelectType, "服装", "Clothing");
            RenameRequired(clonedSelectType, "装饰", "Decoration");
            RenameRequired(clonedSelectType, "食品", "Food");
            RenameRequired(clonedSelectType, "卫生", "Hygiene");
            RenameRequired(clonedSelectType, "其他", "Other");

            CreateCustomModeButton(panel);

            AddClassificationPageFromChild(bar3, clonedSelectThingType, "全部", "AllCustoms", null);
            ReflectionUtil.DestroyIfFound(clonedSelectThingType.transform, "收藏");
            ReflectionUtil.DestroyIfFound(clonedSelectThingType.transform, "方块");

            AddClassificationPageFromChild(bar3, clonedSelectThingType, "建材", "Materials", MaterialsItems);
            AddClassificationPageFromChild(bar3, clonedSelectThingType, "家具", "Furnitures", FurnituresItems);
            AddClassificationPageFromChild(bar3, clonedSelectThingType, "电器", "Electrical", ElectricalItems);
            AddClassificationPageFromChild(bar3, clonedSelectThingType, "植物", "Plant", PlantItems);
            AddClassificationPageFromChild(bar3, clonedSelectThingType, "服装", "Clothing", ClothingItems);
            AddClassificationPageFromChild(bar3, clonedSelectThingType, "装饰", "Decoration", DecorationItems);
            AddClassificationPageFromChild(bar3, clonedSelectThingType, "食物", "Food", FoodItems);
            AddClassificationPageFromChild(bar3, clonedSelectThingType, "卫生", "Hygiene", HygieneItems);
            AddClassificationPageFromChild(bar3, clonedSelectThingType, "其他", "Other", OtherItems);

            EnsureLocalizationTermsForRenamedItems();
        }

        private static void SetupPackageCategory(
            Component panel,
            CustomBagRuntimeState state,
            Component bagCategoryBar,
            Component sourceBar)
        {
            GameObject bagCat4 = ReflectionUtil.InstantiateChild(
                sourceBar.gameObject,
                bagCategoryBar.transform.parent
            );

            if (bagCat4 == null)
                throw new InvalidOperationException("Could not clone BagCategoryBar (4).");

            Component bar4 = bagCat4.GetComponent(_bagCategoryBarType);
            state.BagCategoryBar4 = bar4;
            bagCat4.name = "BagCategoryBar (4)";

            // Preserve the original scene SelectThing reference explicitly.
            GameObject originalSelectThing = GetGameObjectField(sourceBar, "selectThing");
            if (originalSelectThing != null)
                SetGameObjectField(bar4, "selectThing", originalSelectThing);

            GameObject originalSelectType = GetGameObjectField(sourceBar, "selectType");
            GameObject clonedSelectType = CloneGameObject(originalSelectType, "CustomPackageType");
            state.CustomPackageType = clonedSelectType;
            SetGameObjectField(bar4, "selectType", clonedSelectType);

            GameObject originalSelectThingType = GetGameObjectField(sourceBar, "selectThingType");
            GameObject clonedSelectThingType = CloneGameObject(originalSelectThingType, "CustomPackageSelectType");
            state.CustomPackageSelectType = clonedSelectThingType;
            SetGameObjectField(bar4, "selectThingType", clonedSelectThingType);

            GameObject contentObj = RequireChild(clonedSelectType, "Content");
            Component contentTabView = contentObj.GetComponent(_uiTabViewType);
            if (contentTabView == null)
                throw new InvalidOperationException("CustomPackageType/Content has no UITabView.");

            SetMemberRequired(bar4, "uITabView", contentTabView);

            IList tabs = GetTabs(contentTabView);
            for (int index = 12; index >= 3; index--)
                ReflectionUtil.RemoveAtSafe(tabs, index);
            ReflectionUtil.RemoveAtSafe(tabs, 1);

            ClearClassificationPages(bar4);

            // The original package tab "中式" is ONLY the placeholder/template.
            // The actual package tabs are cloned from it below and this placeholder
            // is removed at the end.
            RenameRequired(clonedSelectType, "全部", "AllCustomsPackage");

            string[] packageTabsToDestroy =
            {
                "收藏", "兔子", "冰川", "哥特", "日式", "春节",
                "甜品", "赛博", "雪王", "齿轮", "夏日"
            };

            for (int i = 0; i < packageTabsToDestroy.Length; i++)
                ReflectionUtil.DestroyIfFound(clonedSelectType.transform, packageTabsToDestroy[i]);

            CreateCustomPackageModeButton(panel);

            // ------------------------------------------------------------
            // Create one main package tab per unique package.
            // "中式" is deliberately NOT renamed; it is only the template.
            // ------------------------------------------------------------
            GameObject packageTabTemplate = RequireChild(clonedSelectType, "中式");
            int templateSiblingIndex = packageTabTemplate.transform.GetSiblingIndex();

            HashSet<string> packageNames = new HashSet<string>(
                PackageRegistry.PackageNames,
                StringComparer.OrdinalIgnoreCase
            );

            LocalizationRuntime.EnsureTerms(
                packageNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
            );

            int packageIndex = 0;
            foreach (string packageName in PackageRegistry.PackageNames)
            {
                if (string.IsNullOrWhiteSpace(packageName))
                    continue;

                string cleanPackageName = packageName.Trim();

                GameObject newTab = ReflectionUtil.InstantiateChild(
                    packageTabTemplate,
                    packageTabTemplate.transform.parent
                );

                if (newTab == null)
                    throw new InvalidOperationException(
                        "Could not clone package placeholder tab for " + cleanPackageName
                    );

                newTab.name = cleanPackageName;
                newTab.transform.SetSiblingIndex(templateSiblingIndex + packageIndex + 1);

                AddTab(contentTabView, newTab);

                ConfigurePackageTabSprites(newTab, cleanPackageName);
                ReplacePackageTabLocalizationText(
                    newTab,
                    "UI/" + cleanPackageName
                );

                packageIndex++;
            }

            // Remove the placeholder from UITabView.Tabs and hierarchy.
            RemoveTab(contentTabView, packageTabTemplate);
            UnityEngine.Object.Destroy(packageTabTemplate);

            // ------------------------------------------------------------
            // CustomPackageSelectType: "中式" is also the placeholder.
            // Clone it once per unique package and use each clone as one
            // classification page. The placeholder itself is deleted last.
            // ------------------------------------------------------------
            AddClassificationPageFromChild(
                bar4,
                clonedSelectThingType,
                "全部",
                "AllCustomsPackage",
                null
            );

            GameObject packagePageTemplate = RequireChild(
                clonedSelectThingType,
                "中式"
            );

            int pageSiblingIndex = packagePageTemplate.transform.GetSiblingIndex();
            int pageIndex = 0;

            foreach (string packageName in PackageRegistry.PackageNames)
            {
                if (string.IsNullOrWhiteSpace(packageName))
                    continue;

                string cleanPackageName = packageName.Trim();

                GameObject newPage = ReflectionUtil.InstantiateChild(
                    packagePageTemplate,
                    packagePageTemplate.transform.parent
                );

                if (newPage == null)
                    throw new InvalidOperationException(
                        "Could not clone package classification placeholder for " + cleanPackageName
                    );

                newPage.name = cleanPackageName;
                newPage.transform.SetSiblingIndex(pageSiblingIndex + pageIndex + 1);

                Component newPageTabView = newPage.GetComponent(_uiTabViewType);
                if (newPageTabView == null)
                    throw new InvalidOperationException(
                        "Package classification page '" + cleanPackageName + "' has no UITabView."
                    );

                AddClassificationPage(bar4, newPageTabView);
                pageIndex++;
            }

            for (int i = 0; i < packageTabsToDestroy.Length; i++)
                ReflectionUtil.DestroyIfFound(clonedSelectThingType.transform, packageTabsToDestroy[i]);

            UnityEngine.Object.Destroy(packagePageTemplate);
        }

        private static void AddClassificationPageFromChild(
            Component bagCategoryBar,
            GameObject root,
            string originalName,
            string newName,
            ItemRename[] itemRenames)
        {
            GameObject pageObject = RequireChild(root, originalName);
            Component tabView = pageObject.GetComponent(_uiTabViewType);
            if (tabView == null)
                throw new InvalidOperationException(
                    "Child '" + originalName + "' has no UITabView."
                );

            pageObject.name = newName;
            AddClassificationPage(bagCategoryBar, tabView);

            if (itemRenames != null)
                RenameItems(pageObject.transform, itemRenames);
        }

        private static void CreateCustomModeButton(Component panel)
        {
            Component animator = GetComponentField(panel, "animator");
            if (animator == null)
                throw new InvalidOperationException("animator field is missing.");

            Component tabView = animator.gameObject.GetComponent(_uiTabViewType);
            if (tabView == null)
                throw new InvalidOperationException("animator GameObject has no UITabView.");

            GameObject bagBG = GetTabGameObject(tabView, 1);
            if (bagBG == null)
                throw new InvalidOperationException("UITabView.Tabs[1] is missing.");

            GameObject newButton = ReflectionUtil.InstantiateChild(bagBG, bagBG.transform.parent);
            if (newButton == null)
                throw new InvalidOperationException("Could not clone CustomMode button.");

            newButton.transform.SetSiblingIndex(bagBG.transform.GetSiblingIndex() + 2);
            newButton.name = "CustomMode";
            newButton.transform.position = new Vector3(
                bagBG.transform.position.x,
                bagBG.transform.position.y - 88f * 2f,
                bagBG.transform.position.z
            );

            AddTab(tabView, newButton);

            ConfigureButtonSprites(
                newButton,
                AssetBundleSpriteLoader.CustomNormal,
                AssetBundleSpriteLoader.CustomHighlight,
                AssetBundleSpriteLoader.CustomSelected,
                AssetBundleSpriteLoader.CustomNormal
            );

            LocalizationRuntime.EnsureTerm("UI/CustomMode", "Custom Items");
            ReplaceButtonLocalizationText(newButton, "UI/CustomMode");
        }

        private static void ConfigurePackageTabSprites(
            GameObject buttonObject,
            string packageName)
        {
            object normal = PackageRegistry.LoadSprite(
                packageName,
                "packageIcon"
            );

            object highlighted = PackageRegistry.LoadSprite(
                packageName,
                "packageIcon_Hover"
            );

            object selected = PackageRegistry.LoadSprite(
                packageName,
                "packageIcon_Selected"
            );

            if (normal == null || highlighted == null || selected == null)
            {
                Plugin.LogError(
                    "Package '" + packageName +
                    "' is missing one or more packageIcon sprites."
                );
                return;
            }

            Component image = buttonObject.GetComponent(_imageType);
            if (image != null)
                ReflectionUtil.SetMember(image, "sprite", normal);

            Component button = buttonObject.GetComponent(_buttonType);
            if (button != null)
                SetUnityButtonSpriteState(button, normal, highlighted, selected, normal);

            Component tabButton = buttonObject.GetComponent(_tabButtonType);
            if (tabButton != null)
                SetWaterTabButtonSprites(tabButton, normal, highlighted, selected, normal);
        }

        private static void ReplacePackageTabLocalizationText(
            GameObject tabObject,
            string term)
        {
            if (tabObject == null)
                return;

            if (tabObject.transform.childCount == 0)
            {
                Plugin.LogWarning(
                    "Package tab '" + tabObject.name + "' has no child 0."
                );
                return;
            }

            Transform child0 = tabObject.transform.GetChild(0);
            if (child0.childCount == 0)
            {
                Plugin.LogWarning(
                    "Package tab '" + tabObject.name + "' child 0 has no child 0."
                );
                return;
            }

            Transform textTransform = child0.GetChild(0);
            Component localize = textTransform.GetComponent(_localizeType);
            if (localize == null)
            {
                Plugin.LogWarning(
                    "Package tab '" + tabObject.name +
                    "' child 0/child 0 has no I2.Loc.Localize component."
                );
                return;
            }

            if (!ReflectionUtil.SetMember(localize, "mTerm", term))
            {
                Plugin.LogWarning(
                    "Could not set I2.Loc.Localize.mTerm on package tab '" +
                    tabObject.name + "'."
                );
                return;
            }

            GameObject newText = ReflectionUtil.InstantiateChild(
                localize.gameObject,
                localize.transform.parent
            );

            if (newText == null)
            {
                Plugin.LogWarning(
                    "Could not clone package localization text for '" +
                    tabObject.name + "'."
                );
                return;
            }

            newText.name = localize.gameObject.name;
            UnityEngine.Object.Destroy(localize.gameObject);

            Plugin.Log(
                "Replaced package tab localization text on '" +
                tabObject.name + "' with term '" + term + "'."
            );
        }

        private static void RemoveTab(Component uiTabView, GameObject buttonObject)
        {
            IList tabs = GetTabs(uiTabView);
            if (tabs == null || buttonObject == null)
                return;

            for (int i = tabs.Count - 1; i >= 0; i--)
            {
                Component tabButton = tabs[i] as Component;
                if (tabButton != null && tabButton.gameObject == buttonObject)
                {
                    tabs.RemoveAt(i);
                    return;
                }
            }
        }

        private static void CreateCustomPackageModeButton(Component panel)
        {
            Component animator = GetComponentField(panel, "animator");
            if (animator == null)
                throw new InvalidOperationException("animator field is missing.");

            Component tabView = animator.gameObject.GetComponent(_uiTabViewType);
            if (tabView == null)
                throw new InvalidOperationException("animator GameObject has no UITabView.");

            GameObject bagBG = GetTabGameObject(tabView, 1);
            if (bagBG == null)
                throw new InvalidOperationException("UITabView.Tabs[1] is missing.");

            GameObject newButton = ReflectionUtil.InstantiateChild(bagBG, bagBG.transform.parent);
            if (newButton == null)
                throw new InvalidOperationException("Could not clone CustomPackageMode button.");

            newButton.transform.SetSiblingIndex(bagBG.transform.GetSiblingIndex() + 3);
            newButton.name = "CustomPackageMode";
            newButton.transform.position = new Vector3(
                bagBG.transform.position.x,
                bagBG.transform.position.y - 88f * 3f,
                bagBG.transform.position.z
            );

            AddTab(tabView, newButton);

            ConfigureButtonSprites(
                newButton,
                AssetBundleSpriteLoader.PackageNormal,
                AssetBundleSpriteLoader.PackageHighlight,
                AssetBundleSpriteLoader.PackageSelected,
                AssetBundleSpriteLoader.PackageNormal
            );

            LocalizationRuntime.EnsureTerm("UI/CustomPackageMode", "Custom Packages");
            ReplaceButtonLocalizationText(newButton, "UI/CustomPackageMode");
        }

        private static void ReplaceButtonLocalizationText(GameObject buttonObject, string term)
        {
            if (buttonObject == null)
                throw new ArgumentNullException(nameof(buttonObject));

            Transform firstChild = buttonObject.transform.childCount > 0
                ? buttonObject.transform.GetChild(0)
                : null;

            if (firstChild == null)
            {
                Plugin.LogWarning(
                    "Button '" + buttonObject.name + "' has no child 0 for I2.Loc.Localize."
                );
                return;
            }

            Component localize = firstChild.GetComponent(_localizeType);
            if (localize == null)
            {
                Plugin.LogWarning(
                    "Button '" + buttonObject.name +
                    "' child 0 has no I2.Loc.Localize component."
                );
                return;
            }

            if (!ReflectionUtil.SetMember(localize, "mTerm", term))
            {
                Plugin.LogWarning(
                    "Could not set I2.Loc.Localize.mTerm on '" +
                    buttonObject.name + "'."
                );
                return;
            }

            GameObject newText = ReflectionUtil.InstantiateChild(
                localize.gameObject,
                localize.transform.parent
            );

            if (newText == null)
            {
                Plugin.LogWarning(
                    "Could not clone localized text object for '" +
                    buttonObject.name + "'."
                );
                return;
            }

            newText.name = localize.gameObject.name;
            UnityEngine.Object.Destroy(localize.gameObject);

            Plugin.Log(
                "Replaced button localization text on '" +
                buttonObject.name + "' with term '" + term + "'."
            );
        }

        private static void ConfigureButtonSprites(
            GameObject buttonObject,
            string normalName,
            string highlightedName,
            string selectedName,
            string disabledName,
            object normal,
            object highlighted,
            object selected,
            object disabled)
        {
            if (normal == null || highlighted == null || selected == null || disabled == null)
            {
                Plugin.LogError("One or more runtime sprites are missing. Button sprite setup skipped.");
                return;
            }

            Component image = buttonObject.GetComponent(_imageType);
            if (image != null)
                ReflectionUtil.SetMember(image, "sprite", normal);

            Component button = buttonObject.GetComponent(_buttonType);
            if (button != null)
                SetUnityButtonSpriteState(button, normal, highlighted, selected, disabled);

            Component tabButton = buttonObject.GetComponent(_tabButtonType);
            if (tabButton != null)
                SetWaterTabButtonSprites(tabButton, normal, highlighted, selected, disabled);
        }

        private static void ConfigureButtonSprites(
            GameObject buttonObject,
            string normalName,
            string highlightedName,
            string selectedName,
            string disabledName)
        {
            object normal = AssetBundleSpriteLoader.LoadSprite(normalName);
            object highlighted = AssetBundleSpriteLoader.LoadSprite(highlightedName);
            object selected = AssetBundleSpriteLoader.LoadSprite(selectedName);
            object disabled = AssetBundleSpriteLoader.LoadSprite(disabledName);

            if (normal == null || highlighted == null || selected == null || disabled == null)
            {
                Plugin.LogError("One or more custom button sprites are missing. Button sprite setup skipped.");
                return;
            }

            Component image = buttonObject.GetComponent(_imageType);
            if (image != null)
                ReflectionUtil.SetMember(image, "sprite", normal);

            Component button = buttonObject.GetComponent(_buttonType);
            if (button != null)
                SetUnityButtonSpriteState(button, normal, highlighted, selected, disabled);

            Component tabButton = buttonObject.GetComponent(_tabButtonType);
            if (tabButton != null)
                SetWaterTabButtonSprites(tabButton, normal, highlighted, selected, disabled);
        }

        private static void SetUnityButtonSpriteState(
            Component button,
            object normal,
            object highlighted,
            object selected,
            object disabled)
        {
            object spriteState = ReflectionUtil.GetMember(button, "spriteState");
            if (spriteState == null)
                return;

            ReflectionUtil.SetMember(spriteState, "highlightedSprite", highlighted);
            ReflectionUtil.SetMember(spriteState, "pressedSprite", normal);
            ReflectionUtil.SetMember(spriteState, "selectedSprite", highlighted);
            ReflectionUtil.SetMember(spriteState, "disabledSprite", normal);

            ReflectionUtil.SetMember(button, "spriteState", spriteState);
        }

        private static void SetWaterTabButtonSprites(
            Component tabButton,
            object normal,
            object highlighted,
            object selected,
            object disabled)
        {
            MethodInfo setSprites = AccessTools.Method(
                tabButton.GetType(),
                "SetSprites"
            );

            if (setSprites != null)
            {
                setSprites.Invoke(tabButton, new object[] { normal, highlighted, selected, disabled });
                return;
            }

            object buttonSprite = ReflectionUtil.GetMember(tabButton, "buttonSprite");
            if (buttonSprite == null)
            {
                Plugin.LogWarning(
                    "TabButton.SetSprites() is absent and buttonSprite could not be resolved on " +
                    tabButton.GetType().FullName
                );
                return;
            }

            ReflectionUtil.SetMember(buttonSprite, "normalSprite", normal);
            ReflectionUtil.SetMember(buttonSprite, "highlightSprite", highlighted);
            ReflectionUtil.SetMember(buttonSprite, "selectedSprite", selected);
            ReflectionUtil.SetMember(buttonSprite, "disableSprite", disabled);

            ReflectionUtil.SetMember(tabButton, "buttonSprite", buttonSprite);
        }

        private static void AddTab(Component uiTabView, GameObject buttonObject)
        {
            IList tabs = GetTabs(uiTabView);
            if (tabs == null)
                throw new InvalidOperationException("UITabView.Tabs could not be resolved.");

            Component tabButton = buttonObject.GetComponent(_tabButtonType);
            if (tabButton == null)
                throw new InvalidOperationException("New button has no Water.UI.TabButton.");

            tabs.Add(tabButton);
        }

        private static IList GetTabs(Component uiTabView)
        {
            return ReflectionUtil.GetIListMember(uiTabView, "Tabs");
        }

        private static GameObject GetTabGameObject(Component uiTabView, int index)
        {
            IList tabs = GetTabs(uiTabView);
            if (tabs == null || index < 0 || index >= tabs.Count)
                return null;

            object tabButton = tabs[index];
            return (tabButton as Component)?.gameObject;
        }

        private static void ClearClassificationPages(Component bagCategoryBar)
        {
            ReflectionUtil.ClearList(bagCategoryBar, "classificationPages");
        }

        private static void AddClassificationPage(Component bagCategoryBar, Component tabView)
        {
            if (!ReflectionUtil.AddToList(bagCategoryBar, "classificationPages", tabView))
            {
                throw new InvalidOperationException(
                    "BagCategoryBar.classificationPages could not be resolved."
                );
            }
        }

        private static Component GetComponentField(Component component, string fieldName)
        {
            object value = ReflectionUtil.GetMember(component, fieldName);
            return value as Component;
        }

        private static GameObject GetGameObjectField(Component component, string fieldName)
        {
            object value = ReflectionUtil.GetMember(component, fieldName);
            return value as GameObject;
        }

        private static void SetGameObjectField(Component component, string fieldName, GameObject value)
        {
            if (!ReflectionUtil.SetMember(component, fieldName, value))
            {
                throw new InvalidOperationException(
                    "Could not set " + component.GetType().FullName + "." + fieldName
                );
            }
        }

        private static void SetMemberRequired(object instance, string memberName, object value)
        {
            if (!ReflectionUtil.SetMember(instance, memberName, value))
            {
                throw new InvalidOperationException(
                    "Could not set " + instance.GetType().FullName + "." + memberName
                );
            }
        }

        private static GameObject CloneGameObject(GameObject original, string newName)
        {
            if (original == null)
                throw new InvalidOperationException("Cannot clone null GameObject for " + newName);

            GameObject clone = ReflectionUtil.InstantiateChild(original, original.transform.parent);
            if (clone == null)
                throw new InvalidOperationException("Instantiate returned null for " + newName);

            clone.name = newName;
            return clone;
        }

        private static GameObject RequireChild(GameObject root, string name)
        {
            GameObject child = ReflectionUtil.FindChildByName(root.transform, name);
            if (child == null)
                throw new InvalidOperationException(
                    "Could not find child '" + name + "' below " + root.name
                );

            return child;
        }

        private static void RenameRequired(GameObject root, string originalName, string newName)
        {
            GameObject child = RequireChild(root, originalName);
            child.name = newName;
        }

        private static void RenameItems(Transform root, ItemRename[] renames)
        {
            if (renames == null)
                return;

            for (int i = 0; i < renames.Length; i++)
            {
                ItemRename rename = renames[i];
                GameObject item = ReflectionUtil.FindChildByName(root, rename.OriginalName);
                if (item == null)
                {
                    throw new InvalidOperationException(
                        "Could not find item child '" + rename.OriginalName + "' below " + root.name
                    );
                }

                item.name = rename.NewName;
                if (_tagSlotType == null)
                    continue;

                Component tagSlot = item.GetComponent(_tagSlotType);
                if (tagSlot == null)
                {
                    Plugin.LogWarning(
                        "Item '" + rename.NewName + "' has no Water.Bag.TagSlot component."
                    );
                    continue;
                }

                if (!ReflectionUtil.SetMember(tagSlot, "ItemTag", rename.NewName))
                {
                    Plugin.LogWarning(
                        "Could not set TagSlot.ItemTag for '" + rename.NewName + "'."
                    );
                }
                else if (Plugin.LogVerbose != null && Plugin.LogVerbose.Value)
                {
                    Plugin.Log(
                        "Renamed item '" + rename.OriginalName + "' -> '" +
                        rename.NewName + "'; TagSlot.ItemTag = '" + rename.NewName + "'."
                    );
                }
            }
        }

        private static void EnsureLocalizationTermsForRenamedItems()
        {
            LocalizationRuntime.EnsureTerms(RenamedItemLocalizationNames);
        }

        private static void InitBagCategoryBar(Component bar, string[] classifications, Component panel)
        {
            if (bar == null)
                throw new InvalidOperationException("Custom BagCategoryBar reference is null.");

            MethodInfo onInit = null;
            MethodInfo[] methods = bar.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != "OnInit")
                    continue;

                ParameterInfo[] parameters = methods[i].GetParameters();
                if (parameters.Length == 2)
                {
                    onInit = methods[i];
                    break;
                }
            }

            if (onInit == null)
                throw new InvalidOperationException("BagCategoryBar.OnInit(string[], Action) not found.");

            ParameterInfo[] onInitParams = onInit.GetParameters();
            object callback = CreateCallback(onInitParams[1].ParameterType, panel);

            onInit.Invoke(bar, new object[] { classifications, callback });
        }

        private static object CreateCallback(Type callbackType, Component panel)
        {
            Action action = delegate
            {
                ReflectionUtil.Invoke(panel, "UpdateContent");
            };

            if (callbackType == typeof(Action))
                return action;

            if (typeof(Delegate).IsAssignableFrom(callbackType))
            {
                Delegate created = ReflectionUtil.CreateNoArgDelegate(
                    callbackType,
                    action.Target,
                    action.Method
                );

                if (created != null)
                    return created;
            }

            throw new InvalidOperationException(
                "Unsupported BagCategoryBar.OnInit callback type: " + callbackType.FullName
            );
        }

        private static void ClickBarPostfix(object __instance)
        {
            Component panel = __instance as Component;
            if (panel == null)
                return;

            CustomBagRuntimeState state = panel.GetComponent<CustomBagRuntimeState>();
            if (state == null)
                return;

            InvokeBarVisibility(state.BagCategoryBar3, false);
            InvokeBarVisibility(state.BagCategoryBar4, false);
        }

        private static bool SelectTypeChangePrefix(object __instance, int obj)
        {
            Component panel = __instance as Component;
            if (panel == null)
                return false;

            try
            {
                switch (obj)
                {
                    case 0:
                        ReflectionUtil.Invoke(panel, "ClickBar1");
                        break;

                    case 1:
                        ReflectionUtil.Invoke(panel, "ClickBar2");
                        break;

                    case 2:
                        ReflectionUtil.Invoke(panel, "ClickBar3");
                        break;

                    case 3:
                        ClickBar4(panel);
                        break;

                    default:
                        ClickBar5(panel);
                        break;
                }

                SetCurrentCategory(obj);
                EmitUpdateBagContent();
            }
            catch (Exception ex)
            {
                Plugin.LogError("SelectTypeChange replacement failed: " + ex);
                return true;
            }

            return false;
        }

        private static void ClickBar4(Component panel)
        {
            SetOriginalBarVisibility(panel, "bagCategoryBar", false);
            SetOriginalBarVisibility(panel, "bagCategoryBar_1", false);
            SetOriginalBarVisibility(panel, "bagCategoryBar_2", false);

            CustomBagRuntimeState state = panel.GetComponent<CustomBagRuntimeState>();
            if (state == null)
                return;

            InvokeBarVisibility(state.BagCategoryBar3, true);
            InvokeBarVisibility(state.BagCategoryBar4, false);
            ReflectionUtil.Invoke(panel, "Collapse");
        }

        private static void ClickBar5(Component panel)
        {
            SetOriginalBarVisibility(panel, "bagCategoryBar", false);
            SetOriginalBarVisibility(panel, "bagCategoryBar_1", false);
            SetOriginalBarVisibility(panel, "bagCategoryBar_2", false);

            CustomBagRuntimeState state = panel.GetComponent<CustomBagRuntimeState>();
            if (state == null)
                return;

            InvokeBarVisibility(state.BagCategoryBar3, false);
            InvokeBarVisibility(state.BagCategoryBar4, true);
            ReflectionUtil.Invoke(panel, "Collapse");
        }

        private static void SetOriginalBarVisibility(Component panel, string fieldName, bool show)
        {
            Component bar = GetComponentField(panel, fieldName);
            InvokeBarVisibility(bar, show);
        }

        private static void InvokeBarVisibility(Component bar, bool show)
        {
            if (bar == null)
                return;

            try
            {
                if (show)
                {
                    ReflectionUtil.Invoke(bar, "Show", true, 0);
                }
                else
                {
                    ReflectionUtil.Invoke(bar, "Hide");
                }
            }
            catch (Exception ex)
            {
                Plugin.LogError(
                    "BagCategoryBar visibility call failed (" +
                    (show ? "Show" : "Hide") + "): " + ex
                );
            }
        }

        private static void SetCurrentCategory(int index)
        {
            Type managerBase = _managerBaseGenericType;
            if (managerBase == null || !managerBase.IsGenericTypeDefinition)
                return;

            Type genericManagerType = managerBase.MakeGenericType(_bagManagerType);
            object managerInstance = ReflectionUtil.GetStaticMember(genericManagerType, "Instance");
            if (managerInstance == null)
            {
                Plugin.LogWarning("ManagerBase<BagManager>.Instance could not be resolved.");
                return;
            }

            if (!ReflectionUtil.SetMember(managerInstance, "CurrentCategroy", index))
            {
                Plugin.LogWarning("BagManager.CurrentCategroy could not be set.");
            }
        }

        private static void EmitUpdateBagContent()
        {
            ReflectionUtil.InvokeStatic(_eventManagerType, "EmitEvent", "UpdateBagContent");
        }

        private static void OnDestroyPostfix(object __instance)
        {
            Component panel = __instance as Component;
            if (panel == null)
                return;

            CustomBagRuntimeState state = panel.GetComponent<CustomBagRuntimeState>();
            if (state != null)
                state.ResetState();
        }
    }
}
