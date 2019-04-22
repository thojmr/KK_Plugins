﻿using ExtensibleSaveFormat;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KK_HairAccessoryCustomizer
{
    public partial class KK_HairAccessoryCustomizer
    {
        public class HairAccessoryController : CharaCustomFunctionController
        {
            private Dictionary<int, Dictionary<int, HairAccessoryInfo>> HairAccessories = new Dictionary<int, Dictionary<int, HairAccessoryInfo>>();

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();
                data.data.Add("HairAccessories", MessagePackSerializer.Serialize(HairAccessories));
                SetExtendedData(data);
            }
            protected override void OnReload(GameMode currentGameMode, bool maintainState) => ChaControl.StartCoroutine(LoadData());
            protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
            {
                var data = new PluginData();
                if (HairAccessories.TryGetValue(ChaControl.fileStatus.coordinateType, out var hairAccessoryInfo))
                    if (hairAccessoryInfo.Count > 0)
                        data.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(hairAccessoryInfo));
                    else
                        data.data.Add("CoordinateHairAccessories", null);
                SetCoordinateExtendedData(coordinate, data);
            }
            protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState) => ChaControl.StartCoroutine(LoadCoordinateData(coordinate));
            /// <summary>
            /// Wait one frame for accessories to load and then load the data.
            /// </summary>
            private IEnumerator LoadData()
            {
                ReloadingChara = true;
                yield return null;

                if (MakerAPI.InsideAndLoaded && !MakerAPI.GetCharacterLoadFlags().Clothes) yield break;

                HairAccessories.Clear();

                var data = GetExtendedData();
                if (data != null)
                    if (data.data.TryGetValue("HairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                        HairAccessories = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, HairAccessoryInfo>>>((byte[])loadedHairAccessories);

                if (MakerAPI.InsideAndLoaded)
                {
                    if (InitHairAccessoryInfo(AccessoriesApi.SelectedMakerAccSlot))
                    {
                        //switching to a hair accessory that previously had no data. Meaning this card was made before this plugin. ColorMatch and HairGloss should be off.
                        SetColorMatch(false);
                        SetHairGloss(false);
                    }

                    InitCurrentSlot(this);
                }

                UpdateAccessories();
                ReloadingChara = false;
            }
            /// <summary>
            /// Wait one frame for accessories to load and then load the data.
            /// </summary>
            private IEnumerator LoadCoordinateData(ChaFileCoordinate coordinate)
            {
                ReloadingChara = true;
                yield return null;

                var data = GetCoordinateExtendedData(coordinate);
                if (data != null && data.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                {
                    if (HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType))
                        HairAccessories[ChaControl.fileStatus.coordinateType].Clear();
                    else
                        HairAccessories[ChaControl.fileStatus.coordinateType] = new Dictionary<int, HairAccessoryInfo>();

                    HairAccessories[ChaControl.fileStatus.coordinateType] = MessagePackSerializer.Deserialize<Dictionary<int, HairAccessoryInfo>>((byte[])loadedHairAccessories);
                }

                if (MakerAPI.InsideAndLoaded)
                {
                    if (InitHairAccessoryInfo(AccessoriesApi.SelectedMakerAccSlot))
                    {
                        //switching to a hair accessory that previously had no data. Meaning this card was made before this plugin. ColorMatch and HairGloss should be off.
                        SetColorMatch(false);
                        SetHairGloss(false);
                    }

                    InitCurrentSlot(this);
                }

                UpdateAccessories();
                ReloadingChara = false;
            }
            /// <summary>
            /// Get color match data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetColorMatch(int slot)
            {
                if (HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType) && HairAccessories[ChaControl.fileStatus.coordinateType].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.ColorMatch;

                return ColorMatchDefault;
            }
            /// <summary>
            /// Get color match data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetColorMatch() => GetColorMatch(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Get hair gloss data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetHairGloss(int slot)
            {
                if (HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType) && HairAccessories[ChaControl.fileStatus.coordinateType].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.HairGloss;

                return HairGlossDefault;
            }
            /// <summary>
            /// Get hair gloss data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetHairGloss() => GetHairGloss(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Get outline color data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetOutlineColor(int slot)
            {
                if (HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType) && HairAccessories[ChaControl.fileStatus.coordinateType].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.OutlineColor;

                return OutlineColorDefault;
            }
            /// <summary>
            /// Get outline color data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetOutlineColor() => GetOutlineColor(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Get accessory color data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetAccessoryColor(int slot)
            {
                if (HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType) && HairAccessories[ChaControl.fileStatus.coordinateType].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.AccessoryColor;

                return AccessoryColorDefault;
            }
            /// <summary>
            /// Get accessory color data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetAccessoryColor() => GetAccessoryColor(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Initializes the HairAccessoryInfo for the slot if it is a hair accessory, or removes it if it is not.
            /// </summary>
            /// <returns>True if HairAccessoryInfo was initialized</returns>
            public bool InitHairAccessoryInfo(int slot)
            {
                if (IsHairAccessory(slot))
                {
                    if (!HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType))
                        HairAccessories[ChaControl.fileStatus.coordinateType] = new Dictionary<int, HairAccessoryInfo>();

                    if (!HairAccessories[ChaControl.fileStatus.coordinateType].ContainsKey(slot))
                    {
                        HairAccessories[ChaControl.fileStatus.coordinateType][slot] = new HairAccessoryInfo();
                        return true;
                    }
                    return false;
                }
                else
                {
                    RemoveHairAccessoryInfo(slot);
                    return false;
                }
            }
            /// <summary>
            /// Removes the HairAccessoryInfo for the slot
            /// </summary>
            public void RemoveHairAccessoryInfo(int slot)
            {
                if (HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType))
                    HairAccessories[ChaControl.fileStatus.coordinateType].Remove(slot);
            }
            /// <summary>
            /// Set color match for the specified accessory
            /// </summary>
            public void SetColorMatch(bool value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType) && IsHairAccessory(slot))
                    HairAccessories[ChaControl.fileStatus.coordinateType][slot].ColorMatch = value;
            }
            /// <summary>
            /// Set color match for the current accessory
            /// </summary>
            public void SetColorMatch(bool value) => SetColorMatch(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Set hair gloss for the specified accessory
            /// </summary>
            public void SetHairGloss(bool value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType) && IsHairAccessory(slot))
                    HairAccessories[ChaControl.fileStatus.coordinateType][slot].HairGloss = value;
            }
            /// <summary>
            /// Set hair gloss for the specified accessory
            /// </summary>
            public void SetHairGloss(bool value) => SetHairGloss(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Set outline color for the specified accessory
            /// </summary>
            public void SetOutlineColor(Color value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType) && IsHairAccessory(slot))
                    HairAccessories[ChaControl.fileStatus.coordinateType][slot].OutlineColor = value;
            }
            /// <summary>
            /// Set outline color for the current accessory
            /// </summary>
            public void SetOutlineColor(Color value) => SetOutlineColor(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Set accessory color for the specified accessory
            /// </summary>
            public void SetAccessoryColor(Color value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType) && IsHairAccessory(slot))
                    HairAccessories[ChaControl.fileStatus.coordinateType][slot].AccessoryColor = value;
            }
            /// <summary>
            /// Set accessory color for the current accessory
            /// </summary>
            public void SetAccessoryColor(Color value) => SetAccessoryColor(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Checks if the specified accessory is a hair accessory
            /// </summary>
            public bool IsHairAccessory(ChaAccessoryComponent chaAccessoryComponent) => chaAccessoryComponent?.gameObject.GetComponent<ChaCustomHairComponent>() != null;
            /// <summary>
            /// Checks if the specified accessory is a hair accessory
            /// </summary>
            public bool IsHairAccessory(int slot) => AccessoriesApi.GetAccessory(ChaControl, slot)?.gameObject.GetComponent<ChaCustomHairComponent>() != null;
            /// <summary>
            /// Checks if the specified accessory is a hair accessory and has accessory parts (rendAccessory in the ChaCustomHairComponent MonoBehavior)
            /// </summary>
            public bool HasAccessoryPart(int slot)
            {
                var chaCustomHairComponent = AccessoriesApi.GetAccessory(ChaControl, slot)?.gameObject.GetComponent<ChaCustomHairComponent>();
                if (chaCustomHairComponent != null)
                    foreach (Renderer renderer in chaCustomHairComponent.rendAccessory)
                        if (renderer != null) return true;
                return false;
            }
            internal void CopyAccessoriesHandler(AccessoryCopyEventArgs e)
            {
                if (!HairAccessories.ContainsKey((int)e.CopySource))
                    HairAccessories[(int)e.CopySource] = new Dictionary<int, HairAccessoryInfo>();
                if (!HairAccessories.ContainsKey((int)e.CopyDestination))
                    HairAccessories[(int)e.CopyDestination] = new Dictionary<int, HairAccessoryInfo>();

                foreach (int x in e.CopiedSlotIndexes)
                {
                    if (HairAccessories[(int)e.CopySource].TryGetValue(x, out var hairAccessoryInfo))
                    {
                        //copy hair accessory info to the destination coordinate and slot
                        var newHairAccessoryInfo = new HairAccessoryInfo();
                        newHairAccessoryInfo.ColorMatch = hairAccessoryInfo.ColorMatch;
                        newHairAccessoryInfo.HairGloss = hairAccessoryInfo.HairGloss;
                        newHairAccessoryInfo.OutlineColor = hairAccessoryInfo.OutlineColor;
                        newHairAccessoryInfo.AccessoryColor = hairAccessoryInfo.AccessoryColor;
                        HairAccessories[(int)e.CopyDestination][x] = newHairAccessoryInfo;
                    }
                    else
                        //not a hair accessory, remove hair accessory info from the destination slot
                        HairAccessories[(int)e.CopyDestination].Remove(x);
                }
            }
            internal void TransferAccessoriesHandler(AccessoryTransferEventArgs e)
            {
                if (!HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType)) return;

                if (HairAccessories[ChaControl.fileStatus.coordinateType].TryGetValue(e.SourceSlotIndex, out var hairAccessoryInfo))
                {
                    //copy hair accessory info to the destination slot
                    var newHairAccessoryInfo = new HairAccessoryInfo();
                    newHairAccessoryInfo.ColorMatch = hairAccessoryInfo.ColorMatch;
                    newHairAccessoryInfo.HairGloss = hairAccessoryInfo.HairGloss;
                    newHairAccessoryInfo.OutlineColor = hairAccessoryInfo.OutlineColor;
                    newHairAccessoryInfo.AccessoryColor = hairAccessoryInfo.AccessoryColor;
                    HairAccessories[ChaControl.fileStatus.coordinateType][e.DestinationSlotIndex] = newHairAccessoryInfo;

                    if (AccessoriesApi.SelectedMakerAccSlot == e.DestinationSlotIndex)
                        InitCurrentSlot(this);
                }
                else
                    //not a hair accessory, remove hair accessory info from the destination slot
                    HairAccessories[ChaControl.fileStatus.coordinateType].Remove(e.DestinationSlotIndex);

                UpdateAccessories();
            }
            /// <summary>
            /// Updates all the hair accessories
            /// </summary>
            public void UpdateAccessories()
            {
                if (HairAccessories.ContainsKey(ChaControl.fileStatus.coordinateType))
                    foreach (var x in HairAccessories[ChaControl.fileStatus.coordinateType])
                        UpdateAccessory(x.Key);
            }
            /// <summary>
            /// Updates the specified hair accessory
            /// </summary>
            public void UpdateAccessory(int slot)
            {
                if (!IsHairAccessory(slot)) return;

                ChaAccessoryComponent chaAccessoryComponent = AccessoriesApi.GetAccessory(ChaControl, slot);
                ChaCustomHairComponent chaCustomHairComponent = chaAccessoryComponent?.gameObject.GetComponent<ChaCustomHairComponent>();

                if (!HairAccessories[ChaControl.fileStatus.coordinateType].TryGetValue(slot, out var hairAccessoryInfo)) return;
                if (chaAccessoryComponent?.rendNormal == null) return;
                if (chaCustomHairComponent?.rendHair == null) return;

                if (MakerAPI.InsideAndLoaded && hairAccessoryInfo.ColorMatch)
                {
                    ChaCustom.CvsAccessory cvsAccessory = AccessoriesApi.GetCvsAccessory(slot);
                    cvsAccessory.UpdateAcsColor01(ChaControl.chaFile.custom.hair.parts[0].baseColor);
                    cvsAccessory.UpdateAcsColor02(ChaControl.chaFile.custom.hair.parts[0].startColor);
                    cvsAccessory.UpdateAcsColor03(ChaControl.chaFile.custom.hair.parts[0].endColor);
                    OutlineColorPicker.SetValue(slot, ChaControl.chaFile.custom.hair.parts[0].outlineColor, false);
                    hairAccessoryInfo.OutlineColor = ChaControl.chaFile.custom.hair.parts[0].outlineColor;
                }

                Texture2D texHairGloss = (Texture2D)AccessTools.Property(typeof(ChaControl), "texHairGloss").GetValue(ChaControl, null);

                foreach (Renderer renderer in chaCustomHairComponent.rendHair)
                {
                    if (renderer == null) continue;

                    if (renderer.material.HasProperty(ChaShader._HairGloss))
                    {
                        var mt = renderer.material.GetTexture(ChaShader._MainTex);
                        if (hairAccessoryInfo.HairGloss)
                            renderer.material.SetTexture(ChaShader._HairGloss, texHairGloss);
                        else
                            renderer.material.SetTexture(ChaShader._HairGloss, null);
                        Destroy(mt);
                    }

                    if (renderer.material.HasProperty(ChaShader._LineColor))
                        if (hairAccessoryInfo.ColorMatch)
                            renderer.material.SetColor(ChaShader._LineColor, ChaControl.chaFile.custom.hair.parts[0].outlineColor);
                        else
                            renderer.material.SetColor(ChaShader._LineColor, hairAccessoryInfo.OutlineColor);
                }

                foreach (Renderer renderer in chaCustomHairComponent.rendAccessory)
                {
                    if (renderer == null) continue;

                    if (renderer.material.HasProperty(ChaShader._Color))
                        renderer.material.SetColor(ChaShader._Color, hairAccessoryInfo.AccessoryColor);
                    if (renderer.material.HasProperty(ChaShader._Color2))
                        renderer.material.SetColor(ChaShader._Color2, hairAccessoryInfo.AccessoryColor);
                    if (renderer.material.HasProperty(ChaShader._Color3))
                        renderer.material.SetColor(ChaShader._Color3, hairAccessoryInfo.AccessoryColor);
                }
            }
            /// <summary>
            /// Updates the current hair accessory
            /// </summary>
            public void UpdateAccessory() => UpdateAccessory(AccessoriesApi.SelectedMakerAccSlot);

            [Serializable]
            [MessagePackObject]
            private class HairAccessoryInfo
            {
                [Key("HairGloss")]
                public bool HairGloss = ColorMatchDefault;
                [Key("ColorMatch")]
                public bool ColorMatch = HairGlossDefault;
                [Key("OutlineColor")]
                public Color OutlineColor = OutlineColorDefault;
                [Key("AccessoryColor")]
                public Color AccessoryColor = AccessoryColorDefault;
            }
        }
    }
}
