using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Textures;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using MakePlacePlugin.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static MakePlacePlugin.MakePlacePlugin;

namespace MakePlacePlugin.Gui
{
    public class ConfigurationWindow : Window<MakePlacePlugin>
    {

        public Configuration Config => Plugin.Config;

        private string CustomTag = string.Empty;
        private readonly Dictionary<uint, uint> iconToFurniture = new() { };

        private readonly Vector4 PURPLE = new(0.26275f, 0.21569f, 0.56863f, 1f);
        private readonly Vector4 PURPLE_ALPHA = new(0.26275f, 0.21569f, 0.56863f, 0.5f);

        private FileDialogManager FileDialogManager { get; }

        public ConfigurationWindow(MakePlacePlugin plugin) : base(plugin)
        {
            this.FileDialogManager = new FileDialogManager
            {
                AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking,
            };
        }

        protected void DrawAllUi()
        {
            if (!ImGui.Begin($"{Plugin.Name}", ref WindowVisible, ImGuiWindowFlags.NoScrollWithMouse))
            {
                return;
            }
            if (ImGui.BeginChild("##SettingsRegion"))
            {
                DrawGeneralSettings();
                if (ImGui.BeginChild("##ItemListRegion"))
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, PURPLE_ALPHA);
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, PURPLE);
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, PURPLE);


                    if (ImGui.CollapsingHeader("Interior Furniture", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("interior");
                        DrawItemList(Plugin.InteriorItemList);
                        ImGui.PopID();
                    }
                    if (ImGui.CollapsingHeader("Exterior Furniture", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("exterior");
                        DrawItemList(Plugin.ExteriorItemList);
                        ImGui.PopID();
                    }

                    if (ImGui.CollapsingHeader("Interior Fixtures", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("interiorFixture");
                        DrawFixtureList(Plugin.Layout.interiorFixture);
                        ImGui.PopID();
                    }

                    if (ImGui.CollapsingHeader("Exterior Fixtures", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("exteriorFixture");
                        DrawFixtureList(Plugin.Layout.exteriorFixture);
                        ImGui.PopID();
                    }
                    if (ImGui.CollapsingHeader("Unused Furniture", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("unused");
                        DrawItemList(Plugin.UnusedItemList, true);
                        ImGui.PopID();
                    }

                    ImGui.PopStyleColor(3);
                    ImGui.EndChild();
                }
                ImGui.EndChild();
            }

            this.FileDialogManager.Draw();
        }

        protected override void DrawUi()
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, PURPLE);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PURPLE_ALPHA);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, PURPLE_ALPHA);
            ImGui.SetNextWindowSize(new Vector2(530, 450), ImGuiCond.FirstUseEver);

            DrawAllUi();

            ImGui.PopStyleColor(3);
            ImGui.End();
        }

        #region Basic UI

        private void LogLayoutMode()
        {
            if (Memory.Instance.GetCurrentTerritory() == Memory.HousingArea.Island)
            {
                LogError("(Manage Furnishings -> Place Furnishing Glamours)");
            }
            else
            {
                LogError("(Housing -> Indoor/Outdoor Furnishings)");
            }
        }

        private bool CheckModeForSave()
        {
            if (Memory.Instance.IsHousingMode()) return true;

            LogError("Unable to save layouts outside of Layout mode");
            LogLayoutMode();
            return false;
        }

        private bool CheckModeForLoad()
        {
            if (Config.ApplyLayout && !Memory.Instance.CanEditItem())
            {
                LogError("Unable to load and apply layouts outside of Rotate Layout mode");
                return false;
            }

            if (!Config.ApplyLayout && !Memory.Instance.IsHousingMode())
            {
                LogError("Unable to load layouts outside of Layout mode");
                LogLayoutMode();
                return false;
            }

            return true;
        }

        private void SaveLayoutToFile()
        {
            if (!CheckModeForSave())
            {
                return;
            }

            try
            {
                Plugin.GetGameLayout();
                MakePlacePlugin.LayoutManager.ExportLayout();
            }
            catch (Exception e)
            {
                LogError($"Save Error: {e.Message}", e.StackTrace);
            }
        }

        private void LoadLayoutFromFile()
        {

            if (!CheckModeForLoad()) return;

            try
            {
                SaveLayoutManager.ImportLayout(Config.SaveLocation);
                Log(String.Format("Imported {0} items", Plugin.InteriorItemList.Count + Plugin.ExteriorItemList.Count));

                Plugin.MatchLayout();
                Config.ResetRecord();

                if (Config.ApplyLayout)
                {
                    Plugin.ApplyLayout();
                }

            }
            catch (Exception e)
            {
                LogError($"Load Error: {e.Message}", e.StackTrace);
            }
        }

        unsafe private void DrawGeneralSettings()
        {

            if (ImGui.Checkbox("Label Furniture", ref Config.DrawScreen)) Config.Save();
            if (Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip("Show furniture names on the screen");

            ImGui.SameLine();
            ImGui.Dummy(new Vector2(10, 0));
            ImGui.SameLine();
            if (ImGui.Checkbox("##hideTooltipsOnOff", ref Config.ShowTooltips)) Config.Save();
            ImGui.SameLine();
            ImGui.TextUnformatted("Show Tooltips");

            ImGui.Dummy(new Vector2(0, 10));


            ImGui.Text("Layout");

            if (!Config.SaveLocation.IsNullOrEmpty())
            {
                ImGui.Text($"Current file location: {Config.SaveLocation}");

                if (ImGui.Button("Save"))
                {
                    SaveLayoutToFile();
                }
                if (Config.ShowTooltips && ImGui.IsItemHovered()) ImGui.SetTooltip("Save layout to current file location");
                ImGui.SameLine();

            }

            if (ImGui.Button("Save As"))
            {
                if (CheckModeForSave())
                {
                    string saveName = "save";
                    if (!Config.SaveLocation.IsNullOrEmpty()) saveName = Path.GetFileNameWithoutExtension(Config.SaveLocation);

                    FileDialogManager.SaveFileDialog("Select a Save Location", ".json", saveName, "json", (bool ok, string res) =>
                    {
                        if (!ok)
                        {
                            return;
                        }

                        Config.SaveLocation = res;
                        Config.Save();
                        SaveLayoutToFile();

                    }, Path.GetDirectoryName(Config.SaveLocation));
                }
            }
            if (Config.ShowTooltips && ImGui.IsItemHovered()) ImGui.SetTooltip("Save layout to file");

            ImGui.SameLine(); ImGui.Dummy(new Vector2(20, 0)); ImGui.SameLine();

            if (!Config.SaveLocation.IsNullOrEmpty())
            {
                if (ImGui.Button("Load"))
                {
                    LoadLayoutFromFile();
                }
                if (Config.ShowTooltips && ImGui.IsItemHovered()) ImGui.SetTooltip("Load layout from current file location");
                ImGui.SameLine();
            }

            if (ImGui.Button("Load From"))
            {
                if (CheckModeForLoad())
                {
                    string saveName = "save";
                    if (!Config.SaveLocation.IsNullOrEmpty()) saveName = Path.GetFileNameWithoutExtension(Config.SaveLocation);

                    FileDialogManager.OpenFileDialog("Select a Layout File", ".json", (bool ok, List<string> res) =>
                    {
                        if (!ok)
                        {
                            return;
                        }

                        Config.SaveLocation = res.FirstOrDefault("");
                        Config.Save();

                        LoadLayoutFromFile();

                    }, 1, Path.GetDirectoryName(Config.SaveLocation));
                }
            }
            if (Config.ShowTooltips && ImGui.IsItemHovered()) ImGui.SetTooltip("Load layout from file");

            ImGui.SameLine(); ImGui.Dummy(new Vector2(10, 0)); ImGui.SameLine();

            if (ImGui.Checkbox("Apply Layout", ref Config.ApplyLayout))
            {
                Config.Save();
            }

            ImGui.SameLine(); ImGui.Dummy(new Vector2(10, 0)); ImGui.SameLine();

            ImGui.PushItemWidth(100);
            if (ImGui.InputInt("Placement Interval (ms)", ref Config.LoadInterval))
            {
                Config.Save();
            }
            ImGui.PopItemWidth();
            if (Config.ShowTooltips && ImGui.IsItemHovered()) ImGui.SetTooltip("Time interval between furniture placements when applying a layout. If this is too low (e.g. 200 ms), some placements may be skipped over.");

            ImGui.Dummy(new Vector2(0, 15));

            bool noFloors = Memory.Instance.GetCurrentTerritory() != Memory.HousingArea.Indoors || Memory.Instance.GetIndoorHouseSize().Equals("Apartment");

            if (!noFloors)
            {

                ImGui.Text("Selected Floors");

                if (ImGui.Checkbox("Basement", ref Config.Basement))
                {
                    Config.Save();
                }
                ImGui.SameLine(); ImGui.Dummy(new Vector2(10, 0)); ImGui.SameLine();

                if (ImGui.Checkbox("Ground Floor", ref Config.GroundFloor))
                {
                    Config.Save();
                }
                ImGui.SameLine(); ImGui.Dummy(new Vector2(10, 0)); ImGui.SameLine();

                if (Memory.Instance.HasUpperFloor() && ImGui.Checkbox("Upper Floor", ref Config.UpperFloor))
                {
                    Config.Save();
                }

                ImGui.Dummy(new Vector2(0, 15));

            }

            ImGui.Dummy(new Vector2(0, 15));

        }

        private void DrawRow(int i, HousingItem housingItem, bool showSetPosition = true, int childIndex = -1)
        {
            if (!housingItem.CorrectLocation) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));
            ImGui.Text($"{housingItem.X:N4}, {housingItem.Y:N4}, {housingItem.Z:N4}");
            if (!housingItem.CorrectLocation) ImGui.PopStyleColor();


            ImGui.NextColumn();

            if (!housingItem.CorrectRotation) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));
            ImGui.Text($"{housingItem.Rotate:N3}"); ImGui.NextColumn();
            if (!housingItem.CorrectRotation) ImGui.PopStyleColor();



            var stain = DalamudApi.DataManager.GetExcelSheet<Stain>().GetRow(housingItem.Stain);
            var colorName = stain?.Name;

            if (housingItem.Stain != 0)
            {
                Utils.StainButton("dye_" + i, stain, new Vector2(20));
                ImGui.SameLine();

                if (!housingItem.DyeMatch) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));

                ImGui.Text($"{colorName}");

                if (!housingItem.DyeMatch) ImGui.PopStyleColor();

            }
            else if (housingItem.MaterialItemKey != 0)
            {
                var item = DalamudApi.DataManager.GetExcelSheet<Item>().GetRow(housingItem.MaterialItemKey);
                if (item != null)
                {
                    if (!housingItem.DyeMatch) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));

                    DrawIcon(item.Icon, new Vector2(20, 20));
                    ImGui.SameLine();
                    ImGui.Text(item.Name.ToString());

                    if (!housingItem.DyeMatch) ImGui.PopStyleColor();
                }

            }
            ImGui.NextColumn();

            if (showSetPosition)
            {
                string uniqueID = childIndex == -1 ? i.ToString() : i.ToString() + "_" + childIndex.ToString();

                bool noMatch = housingItem.ItemStruct == IntPtr.Zero;

                if (!noMatch)
                {
                    if (ImGui.Button("Set" + "##" + uniqueID))
                    {
                        Plugin.MatchLayout();

                        if (housingItem.ItemStruct != IntPtr.Zero)
                        {
                            SetItemPosition(housingItem);
                        }
                        else
                        {
                            LogError($"Unable to set position for {housingItem.Name}");
                        }
                    }
                }

                ImGui.NextColumn();
            }
        }

        private void DrawFixtureList(List<Fixture> fixtureList)
        {
            try
            {
                if (ImGui.Button("Clear"))
                {
                    fixtureList.Clear();
                    Config.Save();
                }

                ImGui.Columns(3, "FixtureList", true);
                ImGui.Separator();

                ImGui.Text("Level"); ImGui.NextColumn();
                ImGui.Text("Fixture"); ImGui.NextColumn();
                ImGui.Text("Item"); ImGui.NextColumn();

                ImGui.Separator();

                foreach (var fixture in fixtureList)
                {
                    ImGui.Text(fixture.level); ImGui.NextColumn();
                    ImGui.Text(fixture.type); ImGui.NextColumn();


                    var item = DalamudApi.DataManager.GetExcelSheet<Item>().GetRow(fixture.itemId);
                    if (item != null)
                    {
                        DrawIcon(item.Icon, new Vector2(20, 20));
                        ImGui.SameLine();
                    }
                    ImGui.Text(fixture.name); ImGui.NextColumn();

                    ImGui.Separator();
                }

                ImGui.Columns(1);

            }
            catch (Exception e)
            {
                MakePlacePlugin.LogInfo($"Error in DrawFixtureList: {e.Message}");
                throw;
            }
        }

        private void DrawItemList(List<HousingItem> itemList, bool isUnused = false)
        {



            if (ImGui.Button("Sort"))
            {
                itemList.Sort((x, y) =>
                {
                    if (x.Name.CompareTo(y.Name) != 0)
                        return x.Name.CompareTo(y.Name);
                    if (x.X.CompareTo(y.X) != 0)
                        return x.X.CompareTo(y.X);
                    if (x.Y.CompareTo(y.Y) != 0)
                        return x.Y.CompareTo(y.Y);
                    if (x.Z.CompareTo(y.Z) != 0)
                        return x.Z.CompareTo(y.Z);
                    if (x.Rotate.CompareTo(y.Rotate) != 0)
                        return x.Rotate.CompareTo(y.Rotate);
                    return 0;
                });
                Config.Save();
            }
            ImGui
